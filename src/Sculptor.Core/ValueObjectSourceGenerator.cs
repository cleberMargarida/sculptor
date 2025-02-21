﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Sculptor.Core
{
    [Generator]
    internal class ValueObjectSourceGenerator : IIncrementalGenerator
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
            string? className;
            if (classSymbol.IsGenericType)
            {
                string genericParams = $"<{string.Join(", ", classSymbol.TypeArguments)}>";
                className = $"{classSymbol.Name}{genericParams}";
            }
            else
            {
                className = classSymbol.Name;
            }

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

            var propertyYieldStatements = $"yield return this.{symbols[0].Name};\n";

            propertyYieldStatements += string.Join("\n",
                symbols.Skip(1).Select(static s =>
                $"""
                            yield return this.{s.Name};
                """));

            return $$"""
                // <auto-generated/>
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
}
