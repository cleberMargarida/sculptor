using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Sculptor.Core
{
    /// <summary>
    /// Represents a base class for value objects in the domain model.
    /// </summary>
    public abstract class ValueObject : RichModel
    {
        [HashIgnore]
        private int? _cachedHashCode;

        /// <summary>
        /// Gets the parts of the value object that are used for equality comparison.
        /// </summary>
        /// <returns>An enumerable of objects representing the parts of the value object.</returns>
        protected abstract IEnumerable<object> GetEqualityParts();

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (GetUnproxiedType(this) != GetUnproxiedType(obj))
            {
                return false;
            }

            var valueObject = (ValueObject)obj;

            return GetEqualityParts().SequenceEqual(valueObject.GetEqualityParts());
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            if (!_cachedHashCode.HasValue)
            {
                _cachedHashCode = GetEqualityParts()
                    .Aggregate(1, (current, obj) =>
                    {
                        unchecked
                        {
                            return current * 23 + (obj?.GetHashCode() ?? 0);
                        }
                    });
            }

            return _cachedHashCode.Value;
        }

        /// <summary>
        /// Determines whether two value object instances are equal.
        /// </summary>
        /// <param name="a">The first value object to compare.</param>
        /// <param name="b">The second value object to compare.</param>
        /// <returns>true if the value objects are equal; otherwise, false.</returns>
        public static bool operator ==(ValueObject a, ValueObject b)
        {
            if (a is null && b is null)
            {
                return true;
            }

            if (a is null || b is null)
            {
                return false;
            }

            return a.Equals(b);
        }

        /// <summary>
        /// Determines whether two value object instances are not equal.
        /// </summary>
        /// <param name="a">The first value object to compare.</param>
        /// <param name="b">The second value object to compare.</param>
        /// <returns>true if the value objects are not equal; otherwise, false.</returns>
        public static bool operator !=(ValueObject a, ValueObject b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Gets the unproxied type of the specified object.
        /// </summary>
        /// <param name="obj">The object to get the unproxied type for.</param>
        /// <returns>The unproxied type of the object.</returns>
        internal static Type GetUnproxiedType(object obj)
        {
            const string EFCoreProxyPrefix = "Castle.Proxies.";
            const string NHibernateProxyPostfix = "Proxy";

            Type type = obj.GetType();
            string typeString = type.ToString();

            if (typeString.Contains(EFCoreProxyPrefix) || typeString.EndsWith(NHibernateProxyPostfix))
            {
                return type.BaseType;
            }

            return type;
        }
    }

    [Generator()]
    public class ValueObjectSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Collect candidate classes that might inherit from ValueObject
            var classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: IsClassWithBaseList,
                    transform: GetClassDeclaration)
                .Where(static m => m is not null);

            // Combine the semantic model with class declarations
            var classSymbols = context.CompilationProvider
                .Combine(classDeclarations.Collect())
                .SelectMany(static (tuple, _) =>
                {
                    var (compilation, classes) = tuple;
                    var results = new List<INamedTypeSymbol>();

                    foreach (var classDeclaration in classes)
                    {
                        var semanticModel = compilation.GetSemanticModel(classDeclaration!.SyntaxTree);
                        var symbol = semanticModel.GetDeclaredSymbol(classDeclaration);

                        if (symbol != null && !symbol.IsAbstract && InheritsFromValueObject(symbol))
                        {
                            results.Add(symbol);
                        }
                    }

                    return results;
                });

            // Generate code for each eligible class
            context.RegisterSourceOutput(classSymbols, (spc, classSymbol) =>
            {
                var sourceCode = GenerateEqualityPartsImplementation(classSymbol);
                spc.AddSource($"{classSymbol.Name}_Generated.cs", SourceText.From(sourceCode, Encoding.UTF8));
            });

        }

        private static bool IsClassWithBaseList(SyntaxNode node, CancellationToken _)
        {
            return node is ClassDeclarationSyntax classDecl && classDecl.BaseList != null;
        }

        private static ClassDeclarationSyntax? GetClassDeclaration(GeneratorSyntaxContext context, CancellationToken _)
        {
            return context.Node as ClassDeclarationSyntax;
        }

        private static bool InheritsFromValueObject(INamedTypeSymbol classSymbol)
        {
            while (classSymbol.BaseType != null)
            {
                if (classSymbol.BaseType.Name is "ValueObject" or "Sculptor.Core.ValueObject")
                {
                    return true;
                }
                classSymbol = classSymbol.BaseType;
            }
            return false;
        }

        private static string GenerateEqualityPartsImplementation(INamedTypeSymbol classSymbol)
        {
            var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
            var className = classSymbol.Name;
            var symbols = new List<ISymbol>();

            var currentClass = classSymbol;

            while (currentClass != null)
            {
                var members = currentClass.GetMembers();

                var properties = members.OfType<IPropertySymbol>();
                var fields = members.OfType<IFieldSymbol>();

                symbols.AddRange(properties.Where(static 
                    p => 
                        p.DeclaredAccessibility == Accessibility.Public
                        && !p.IsStatic
                        && !p.IsAbstract
                        && !p.GetAttributes().Any(static t => t.AttributeClass?.Name == nameof(HashIgnore))));

                symbols.AddRange(fields.Where(static 
                    p => 
                        p.DeclaredAccessibility == Accessibility.Private 
                        && !p.IsStatic
                        && !p.IsImplicitlyDeclared
                        && !p.GetAttributes().Any(static t => t.AttributeClass?.Name == nameof(HashIgnore))));

                currentClass = currentClass.BaseType;
            }

            var propertyYieldStatements = string.Join(string.Empty, symbols.Select(p => $"yield return this.{p.Name};"));

            return $$"""

                using System;
                using System.Collections.Generic;

                namespace {{namespaceName}}
                {
                    partial class {{className}}
                    {
                        protected override IEnumerable<object> GetEqualityParts()
                        {
                            {{propertyYieldStatements}}
                        }
                    }
                }

                """;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class HashIgnore : Attribute
    {
    }
}
