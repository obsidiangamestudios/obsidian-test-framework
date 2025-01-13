using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace ObsidianGameStudios.NUnit.Database.SourceGenerators;

public static class RoslynUtils
{
    public static bool IsPartial(this IMethodSymbol methodSymbol)
    {
        return methodSymbol.DeclaringSyntaxReferences
            .Select(syntaxRef => syntaxRef.GetSyntax())
            .OfType<MethodDeclarationSyntax>()
            .Any(methodSyntax => methodSyntax.Modifiers.Any(SyntaxKind.PartialKeyword));
    }

    public static object? GetAttributeValue(
        this SeparatedSyntaxList<AttributeArgumentSyntax> arguments,
        SemanticModel semanticModel,
        int argumentIndex
    )
    {
        if (arguments.Count <= argumentIndex)
            return null;
        var argument = arguments[argumentIndex]?.Expression;
        if (argument == null)
            return null;

        return semanticModel.GetConstantValue(argument).Value;
    }

    public static bool TryGetClassSymbol(
        this ClassDeclarationSyntax classDeclarationSyntax,
        SemanticModel semanticModel,
        out INamedTypeSymbol? classSymbol
    )
    {
        classSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax) as INamedTypeSymbol;
        return classSymbol != null;
    }

    public static bool IsInheritingFromGameComponent(
        this ClassDeclarationSyntax classDeclarationSyntax,
        SemanticModel semanticModel
    )
    {
        return IsInheritingFrom(
            classDeclarationSyntax,
            semanticModel,
            "Obsidian.Framework.GameComponent"
        );
    }

    public static bool IsInheritingFromGameObject(
        this ClassDeclarationSyntax classDeclarationSyntax,
        SemanticModel semanticModel
    )
    {
        return IsInheritingFrom(
            classDeclarationSyntax,
            semanticModel,
            "Obsidian.Framework.GameObject"
        );
    }

    public static bool IsInheritingFrom(
        this ClassDeclarationSyntax classDeclarationSyntax,
        SemanticModel semanticModel,
        string fullTypeName
    )
    {
        if (classDeclarationSyntax.TryGetClassSymbol(semanticModel, out var classSymbol) is false)
            return false;

        INamedTypeSymbol? baseType = classSymbol.BaseType;
        while (baseType != null)
        {
            if (baseType.ToDisplayString() == fullTypeName)
                return true;

            baseType = baseType.BaseType;
        }

        return false;
    }

    public static bool TryGetAttributeMethodSymbol(
        this GeneratorSyntaxContext context,
        AttributeSyntax attributeSyntax,
        out IMethodSymbol? methodSymbol
    )
    {
        if (
            context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol
            is not IMethodSymbol attrSymbol
        )
        {
            methodSymbol = null;
            return false;
        }
        methodSymbol = attrSymbol;
        return true;
    }
}
