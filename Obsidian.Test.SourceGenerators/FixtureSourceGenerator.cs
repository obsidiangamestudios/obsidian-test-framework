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

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context
            .SyntaxProvider.CreateSyntaxProvider(
                (s, _) => s is ClassDeclarationSyntax,
                (ctx, _) => GetClassDeclarationForSourceGen(ctx)
            )
            .Where(c => c is { ConnectionStringName: not null });
        // Generate the source code.
        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(provider.Collect()),
            ((ctx, t) => GenerateCode(ctx, t.Left, t.Right))
        );
    }

    private enum DatabaseProvider
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

            if (!classDeclarationSyntax.TryGetClassSymbol(semanticModel, out var classSymbol))
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
                var code = GetSqlServerFixture(classInfo, compilation.AssemblyName);

                sb.AppendLine(code);
            }

            sb.AppendLine("}");

            ctx.AddSource($"{className}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }
    }

    private static string GetSqlServerFixture(ClassInfo classInfo, string assemblyName)
    {
        var prefix = assemblyName.Replace(".", "_").ToLowerInvariant();
        string code =
            $@"
 public static DatabaseIntegrationFixtureSqlServer DatabaseFixture;

    public {classInfo.Class.Identifier.Text}()
    {{
        string connString = Environment.GetEnvironmentVariable(""{classInfo.ConnectionStringName}"") ?? throw new Exception(""{classInfo.ConnectionStringName} environment variable not set."");
        DatabaseFixture = new DatabaseIntegrationFixtureSqlServer(connString, {classInfo.PoolSize}, ""{prefix}"", {classInfo.DropDatabase.ToString().ToLowerInvariant()});
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

    struct ClassInfo
    {
        public ClassDeclarationSyntax Class { get; set; }
        public string? ConnectionStringName { get; set; }
        public DatabaseProvider Provider { get; set; }
        public int PoolSize { get; set; }

        public bool DropDatabase { get; set; }
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

                    var connectionString =
                        attributeSyntax.ArgumentList?.Arguments.GetAttributeValue(
                            context.SemanticModel,
                            0
                        );
                    var provider = attributeSyntax.ArgumentList?.Arguments.GetAttributeValue(
                        context.SemanticModel,
                        1
                    );

                    var poolSize = attributeSyntax.ArgumentList?.Arguments.GetAttributeValue(
                        context.SemanticModel,
                        2
                    );

                    var dropDatabase = attributeSyntax.ArgumentList?.Arguments.GetAttributeValue(
                        context.SemanticModel,
                        3
                    );

                    dropDatabase ??= false;

                    classInfo.ConnectionStringName = connectionString?.ToString();
                    classInfo.Provider = (DatabaseProvider)provider;
                    classInfo.PoolSize = (int)poolSize;
                    classInfo.DropDatabase = (bool)dropDatabase;
                }
            }
        }

        return classInfo;
    }
}