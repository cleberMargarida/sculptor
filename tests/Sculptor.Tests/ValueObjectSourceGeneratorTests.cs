using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Sculptor.Core;

namespace Sculptor.Tests
{
    public class ValueObjectSourceGeneratorTests
    {
        [Fact]
        public void GenerateEqualityPartsMethod_WhenCalled_GeneratesExpectedCode()
        {
            // Arrange
            var source = """
                using Sculptor.Core;

                public partial class Foo : ValueObject
                {
                    public int Bar { get; set; }
                }
                """;

            var expectedGeneratedCode = """

                using System;
                using System.Collections.Generic;

                namespace <global namespace>
                {
                    partial class Foo
                    {
                        protected override IEnumerable<object> GetEqualityParts()
                        {
                            yield return this.Bar;
                        }
                    }
                }

                """;

            // Act
            var actualGeneratedCode = CSharpGeneratorDriver.Create(new ValueObjectSourceGenerator())
                .RunGeneratorsAndUpdateCompilation(CSharpCompilation.Create("CSharpCodeGen.GenerateAssembly")
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(source))
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)), out _, out var _)
                .GetRunResult()
                .GeneratedTrees.First()
                .ToString();

            // Assert
            Assert.Equal(expectedGeneratedCode, actualGeneratedCode);
        }
    }
}
