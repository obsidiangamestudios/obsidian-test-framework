using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Obsidian.Test.SourceGenerators;

[Generator]
public class TestSuiteSourceGenerator : IIncrementalGenerator
{
    private const string FixtureAttributeName = "DatabaseTestSuiteAttribute";

    private const string Namespace = "Obsidian.Test.Framework";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context
            .SyntaxProvider.CreateSyntaxProvider(
                (s, _) => s is ClassDeclarationSyntax,
                (ctx, _) => GetClassDeclarationForSourceGen(ctx)
            )
            .Where(c => c is { FullFixtureTypeName: not null });
        // Generate the source code.
        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(provider.Collect()),
            ((ctx, t) => GenerateCode(ctx, t.Left, t.Right))
        );
    }

    public enum DatabaseProvider
    {
        SqlServer = 10,
        Postgres = 20,
    }

    private void GenerateCode(
        SourceProductionContext ctx,
        Compilation compilation,
        ImmutableArray<ClassInfo> classes
    )
    {
        foreach (var classInfo in classes)
        {
            var classDeclarationSyntax = classInfo.Class;

            StringBuilder sb = new();

            var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);

            if (
                classDeclarationSyntax.TryGetClassSymbol(semanticModel, out var classSymbol)
                is false
            )
                continue;

            var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
            var className = classDeclarationSyntax.Identifier.Text;

            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("using Obsidian.Test.Framework;");

            sb.AppendLine($"namespace {namespaceName};");
            sb.AppendLine($"partial class {className}");
            sb.AppendLine("{");

            var code = GetTestPartialClass(classInfo);

            sb.AppendLine(code);

            sb.AppendLine("}");

            ctx.AddSource($"{className}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }
    }

    private static string GetTestPartialClass(ClassInfo classInfo)
    {
        string code =
            $@"
 
    protected DbInfo CurrentDbInfo {{ get; set; }}

    [SetUp]
    public async Task Setup()
    {{
        CurrentDbInfo = {classInfo.FullFixtureTypeName}.DatabaseFixture.TakeOne();
        await OnSetupAsync();
        await {classInfo.FullFixtureTypeName}.DatabaseFixture.ResetDatabaseAsync(CurrentDbInfo);
    }}

    [TearDown]
    public async Task TearDown()
    {{
        {classInfo.FullFixtureTypeName}.DatabaseFixture.ReturnOne(CurrentDbInfo);
        await OnTearDownAsync();
    }}


    public async ValueTask DisposeAsync()
    {{
        await OnDisposeAsync();
    }}

    protected partial Task OnDisposeAsync();
    protected partial Task OnTearDownAsync();
    protected partial Task OnSetupAsync();
";
        return code;
    }

    struct ClassInfo
    {
        public ClassDeclarationSyntax Class { get; set; }
        public string FullFixtureTypeName { get; set; }
    }

    private static ClassInfo GetClassDeclarationForSourceGen(GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

        ClassInfo classInfo = new() { Class = classDeclarationSyntax };
        // Find all attributes with the name DataProperty and return the class declaration and the field declaration.
        foreach (var attributeListSyntax in classDeclarationSyntax.AttributeLists)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                if (!context.TryGetAttributeMethodSymbol(attributeSyntax, out var attributeSymbol))
                    continue; // if we can't get the symbol, ignore it

                string attributeName = attributeSymbol.ContainingType.ToDisplayString();

                // Check the full name of the DataProperty attribute.
                if (attributeName == $"Obsidian.Test.Framework.{FixtureAttributeName}")
                {
                    // get all attributes values with the name DatabaseFixtureGenerator and return the class declaration and the field declaration.
                    var typeArgument = attributeSyntax.ArgumentList?.Arguments.Single();
                    string? typeValue = null;
                    if (typeArgument.Expression is TypeOfExpressionSyntax typeOfExpression)
                    {
                        var typeInfo = context.SemanticModel.GetTypeInfo(typeOfExpression.Type);
                        typeValue = typeInfo.Type?.ToDisplayString();
                    }

                    classInfo.FullFixtureTypeName = typeValue;
                }
            }
        }

        return classInfo;
    }
}
