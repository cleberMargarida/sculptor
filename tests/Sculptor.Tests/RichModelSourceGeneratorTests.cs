﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Sculptor.Core;

namespace Sculptor.Tests;

public class RichModelSourceGeneratorTests
{
    [Fact]
    public void GenerateMethod_WhenCalled_GeneratesExpectedCode()
    {
        // Arrange
        var source = @"""
            using Microsoft.Extensions.DependencyInjection;
            public partial class Foo : Sculptor.Core.RichModel
            {
                public void DoSomething([FromServices] IDbConnection dbConnection, ILogger<Foo> logger) {}
            }
            """;

        var expectedGeneratedCode = """
            using Microsoft.Extensions.DependencyInjection;
            namespace <global namespace> {
                public partial class Foo {
                    public void DoSomething(ILogger<Foo> logger) {
                        DoSomething(Services.GetRequiredService<IDbConnection>(), logger);
                    }
                }
            }
            """;

        // Act
        var actualGeneratedCode = CSharpGeneratorDriver.Create(new RichModelSourceGenerator())
            .RunGeneratorsAndUpdateCompilation(CSharpCompilation.Create("CSharpCodeGen.GenerateAssembly")
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(source))
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)), out _, out var _)
            .GetRunResult()
            .GeneratedTrees.First()
            .ToString();

        // Assert
        Assert.Contains(expectedGeneratedCode, actualGeneratedCode);
    }
}