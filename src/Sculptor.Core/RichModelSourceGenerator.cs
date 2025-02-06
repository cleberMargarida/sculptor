using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Sculptor.Core
{
    [Generator]
    internal class RichModelSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var provider = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => IsCandidateClass(s),
                    transform: static (ctx, _) => GetClassWithMethods(ctx))
                .Where(static model => model is not null);

            context.RegisterSourceOutput(provider, static (spc, model) =>
            {
                if (model is not null)
                {
                    var source = GeneratePartialClass(model);
                    spc.AddSource($"{model.ClassSymbol.Name}_Generated.g.cs", SourceText.From(source, Encoding.UTF8));
                }
            });
        }

        private static bool IsCandidateClass(SyntaxNode node)
        {
            return node is ClassDeclarationSyntax classDecl && classDecl.BaseList != null;
        }

        private static ClassModel? GetClassWithMethods(GeneratorSyntaxContext context)
        {
            var classDecl = (ClassDeclarationSyntax)context.Node;
            var semanticModel = context.SemanticModel;

            if (semanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol classSymbol || !InheritsFromRichModel(classSymbol))
            {
                return null;
            }

            var methods = classDecl.Members
                .OfType<MethodDeclarationSyntax>()
                .Select(method => new MethodModel(method, classSymbol))
                .Where(m => m.HasFromServicesParameter)
                .ToImmutableArray();

            return methods.Length > 0 ? new ClassModel(classSymbol, methods) : null;
        }

        private static bool InheritsFromRichModel(INamedTypeSymbol classSymbol)
        {
            INamedTypeSymbol? baseType = classSymbol.BaseType;

            while (baseType != null)
            {
                string baseTypeName = baseType.ToString();

                if (baseTypeName is "Sculptor.Core.RichModel" or "Core.RichModel" or "RichModel")
                {
                    return true;
                }

                baseType = baseType.BaseType;
            }

            return false;
        }

        private static string GeneratePartialClass(ClassModel model)
        {
            var sb = new StringBuilder();

            var usingDirectives = model.ClassSymbol.DeclaringSyntaxReferences
                .Select(r => r.GetSyntax())
                .OfType<ClassDeclarationSyntax>()
                .Select(c => c.SyntaxTree.GetRoot())
                .OfType<CompilationUnitSyntax>()
                .SelectMany(cu => cu.Usings)
                .Distinct()
                .Select(u => u.ToString());

            foreach (var directive in usingDirectives)
            {
                sb.AppendLine(directive);
            }

            sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");

            sb.AppendLine($$"""
                namespace {{model.ClassSymbol.ContainingNamespace}} {
                    public partial class {{model.ClassSymbol.Name}} {
                """);

            foreach (var method in model.Methods)
            {
                sb.AppendLine($$"""
                            public {{method.ReturnType}} {{method.MethodName}}({{string.Join(", ", method.RequiredParameters)}}) {
                                {{method.MethodName}}({{string.Join(", ", method.InnerParameters)}});
                            }
                    """);
            }

            sb.AppendLine($$"""
                    }
                }
                """);

            return sb.ToString();

        }

        [DebuggerDisplay("{ClassSymbol}")]
        private class ClassModel(INamedTypeSymbol classSymbol, ImmutableArray<MethodModel> methods)
        {
            public INamedTypeSymbol ClassSymbol { get; } = classSymbol;
            public ImmutableArray<MethodModel> Methods { get; } = methods;
        }

        [DebuggerDisplay("{MethodName}")]
        private record MethodModel
        {
            public string MethodName { get; }
            public string ReturnType { get; }
            public bool HasFromServicesParameter { get; }
            public ImmutableArray<string> RequiredParameters { get; }
            public ImmutableArray<string> InnerParameters { get; }

            public MethodModel(MethodDeclarationSyntax method, INamedTypeSymbol classSymbol)
            {
                MethodName = method.Identifier.Text;
                ReturnType = method.ReturnType.ToString();

                var parameters = method.ParameterList.Parameters;

                HasFromServicesParameter = false;

                var defaultParameterCallsBuilder = ImmutableArray.CreateBuilder<string>();
                var requiredParametersBuilder = ImmutableArray.CreateBuilder<string>();

                foreach (var parameter in parameters)
                {
                    if (HasFromServicesAttribute(parameter))
                    {
                        HasFromServicesParameter = true;
                        defaultParameterCallsBuilder.Add($"Services.GetRequiredService<{parameter.Type}>()");
                    }
                    else
                    {
                        defaultParameterCallsBuilder.Add(parameter.Identifier.Text);
                        requiredParametersBuilder.Add($"{parameter.Type} {parameter.Identifier.Text}");
                    }
                }

                InnerParameters = defaultParameterCallsBuilder.ToImmutable();
                RequiredParameters = requiredParametersBuilder.ToImmutable();
            }


            private static bool HasFromServicesAttribute(ParameterSyntax parameter)
            {
                return parameter.AttributeLists
                    .SelectMany(a => a.Attributes)
                    .Any(a => a.Name.ToString() == "FromServices");
            }
        }
    }
}