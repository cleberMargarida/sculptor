using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
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

            var classHasFromServicesAttributes = false;
            var methods = new List<MethodDeclarationSyntax>();

            foreach (var method in classDecl.Members.OfType<MethodDeclarationSyntax>())
            {
                var newMethod = method;
                var parameterList = newMethod.ParameterList;

                if (!parameterList.Parameters.Any(param => param.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.Name.ToString() == "FromServices"))) 
                {
                    continue;
                }

                var argumentList = SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(
                        parameterList.Parameters.Select(param =>
                        {
                            if (param.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.Name.ToString() == "FromServices"))
                            {
                                classHasFromServicesAttributes = true;
                                newMethod = newMethod.RemoveNode(param, SyntaxRemoveOptions.KeepLeadingTrivia);
                                return SyntaxFactory.Argument(
                                    SyntaxFactory.InvocationExpression(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.IdentifierName("Services"),
                                            SyntaxFactory.GenericName("GetRequiredService")
                                                .WithTypeArgumentList(
                                                    SyntaxFactory.TypeArgumentList(
                                                        SyntaxFactory.SingletonSeparatedList(param.Type!)
                                                    )
                                                )
                                        )
                                    )
                                );
                            }
                            return SyntaxFactory.Argument(SyntaxFactory.IdentifierName(param.Identifier));
                        })
                    )
                ).NormalizeWhitespace("", eol: " ");

                if (!classHasFromServicesAttributes)
                {
                    return null;               
                }

                newMethod = newMethod.WithBody(null).WithExpressionBody(
                    SyntaxFactory.ArrowExpressionClause(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.IdentifierName(newMethod.Identifier)
                        ).WithArgumentList(argumentList)
                    ))
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                    .NormalizeWhitespace("", eol: " ");

                methods.Add(newMethod);
            }

            return new ClassModel(classSymbol, methods.ToImmutableArray());
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
            //Debugger.Launch();
            var sb = new StringBuilder();

            var usingDirectives = model.ClassSymbol.DeclaringSyntaxReferences
                .Select(r => r.GetSyntax())
                .OfType<ClassDeclarationSyntax>()
                .Select(c => c.SyntaxTree.GetRoot())
                .OfType<CompilationUnitSyntax>()
                .SelectMany(cu => cu.Usings)
                .Distinct()
                .Select(u => u.ToString());

            sb.AppendLine("// <auto-generated/>");

            foreach (var directive in usingDirectives)
            {
                sb.AppendLine(directive);
            }

            sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");

            if (model.ClassSymbol.IsGenericType)
            {
                string genericParams = $"<{string.Join(", ", model.ClassSymbol.TypeParameters)}>";

                sb.AppendLine($$"""
                namespace {{model.ClassSymbol.ContainingNamespace}} {
                    public partial class {{model.ClassSymbol.Name}}{{genericParams}} {
                """);
            }
            else
            {
                sb.AppendLine($$"""
                namespace {{model.ClassSymbol.ContainingNamespace}} {
                    public partial class {{model.ClassSymbol.Name}} {
                """);
            }

            foreach (var method in model.Methods)
            {
                sb.AppendLine($$"""
                            {{string.Join("\t\t", method.ToFullString().Split('\n'))}}
                    """);
            }

            sb.AppendLine($$"""
                    }
                }
                """);

            return sb.ToString();

        }

        [DebuggerDisplay("{ClassSymbol}")]
        private class ClassModel(INamedTypeSymbol classSymbol, ImmutableArray<MethodDeclarationSyntax> methods)
        {
            public INamedTypeSymbol ClassSymbol { get; } = classSymbol;
            public ImmutableArray<MethodDeclarationSyntax> Methods { get; } = methods;
        }
    }
}