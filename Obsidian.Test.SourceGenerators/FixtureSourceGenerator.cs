using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Obsidian.Test.SourceGenerators;

[Generator]
public class FixtureSourceGenerator : IIncrementalGenerator
{
    private const string FixtureAttributeName = "DatabaseFixtureGeneratorAttribute";

    private const string Namespace = "Obsidian.Test.Framework";
//
//     private const string FixtureAttributeSourceCode = $@"// <auto-generated/>
//
// namespace {Namespace}
// {{
//     [System.AttributeUsage(System.AttributeTargets.Class)]
//     public class {FixtureAttributeName} : System.Attribute
//     {{
//     }}
// }}";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {

        // context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
        //     "DatabaseFixtureGenerator.g.cs",
        //     SourceText.From(FixtureAttributeSourceCode, Encoding.UTF8)));

        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
                (s, _) => s is ClassDeclarationSyntax,
                (ctx, _) => GetClassDeclarationForSourceGen(ctx))
            .Where(c => c is { ConnectionStringName: not null });
        // Generate the source code.
        context.RegisterSourceOutput(context.CompilationProvider.Combine(provider.Collect()),
            ((ctx, t) => GenerateCode(ctx, t.Left, t.Right)));
    }

    public enum DatabaseProvider
    {
        SqlServer = 10,
        Postgres = 20,
    }

    private void GenerateCode(SourceProductionContext ctx, Compilation compilation,
        ImmutableArray<ClassInfo> classes)
    {
        foreach (var classInfo in classes)
        {
            var classDeclarationSyntax = classInfo.Class;

            StringBuilder sb = new();

            var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);

            if (classDeclarationSyntax.TryGetClassSymbol(semanticModel, out var classSymbol) is false)
               continue;

            var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
            var className = classDeclarationSyntax.Identifier.Text;

            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("using Obsidian.Test.Framework;");
            if (classInfo.Provider == DatabaseProvider.SqlServer)
            {
                sb.AppendLine("using Obsidian.Test.Framework.SqlServer;");
            }
            sb.AppendLine($"namespace {namespaceName};");
            sb.AppendLine("[SetUpFixture]");
            sb.AppendLine($"partial class {className}");
            sb.AppendLine("{");
            if (classInfo.Provider == DatabaseProvider.SqlServer)
            {
                var code = GetSqlServerFixture(classInfo);

                sb.AppendLine(code);
            }

            sb.AppendLine("}");

            ctx.AddSource($"{className}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }
    }

    private static string GetSqlServerFixture(ClassInfo classInfo)
    {
        string  code = $@"
 public static DatabaseIntegrationFixtureSqlServer DatabaseFixture;

    public {classInfo.Class.Identifier.Text}()
    {{
        string connString = Environment.GetEnvironmentVariable(""{classInfo.ConnectionStringName}"") ?? throw new Exception(""{classInfo.ConnectionStringName} environment variable not set."");
        DatabaseFixture = new DatabaseIntegrationFixtureSqlServer(connString, {classInfo.PoolSize}, ""{classInfo.ProjectPrefix}"");
    }}

    [OneTimeSetUp]
    public async Task RunBeforeAnyTests()
    {{
        await DatabaseFixture.InitializeAsync();
    }}

    [OneTimeTearDown]
    public async Task RunAfterAllTests()
    {{
        await DatabaseFixture.DisposeAsync();
    }}
";
        return code;
    }


    struct PropertyInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

    struct ClassInfo
    {
        public ClassDeclarationSyntax Class { get; set; }
        public string ConnectionStringName { get; set; }
        public DatabaseProvider Provider { get; set; }
        public string ProjectPrefix { get; set; }
        public int PoolSize { get; set; }
    }

    private static ClassInfo GetClassDeclarationForSourceGen(
        GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

        ClassInfo classInfo = new()
        {
            Class = classDeclarationSyntax,
        };
        // Find all attributes with the name DataProperty and return the class declaration and the field declaration.
        foreach (var attributeListSyntax in classDeclarationSyntax.AttributeLists)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                    continue; // if we can't get the symbol, ignore it

                string attributeName = attributeSymbol.ContainingType.ToDisplayString();

                // Check the full name of the DataProperty attribute.
                if (attributeName == $"Obsidian.Test.Framework.{FixtureAttributeName}")
                {
                    // get all attributes values with the name DatabaseFixtureGenerator and return the class declaration and the field declaration.

                    var connectionString =
                        attributeSyntax.ArgumentList?.Arguments.GetAttributeValue(context.SemanticModel, 0);
                    var provider = attributeSyntax.ArgumentList?.Arguments.GetAttributeValue(context.SemanticModel, 1);
                    var projectPrefix =
                        attributeSyntax.ArgumentList?.Arguments.GetAttributeValue(context.SemanticModel, 2);
                    var poolSize = attributeSyntax.ArgumentList?.Arguments.GetAttributeValue(context.SemanticModel, 3);

                    classInfo.ConnectionStringName = connectionString?.ToString();
                    classInfo.Provider = (DatabaseProvider)provider;
                    classInfo.ProjectPrefix = projectPrefix?.ToString();
                    classInfo.PoolSize = (int)poolSize;
                }
            }
        }

        return classInfo;
    }
}