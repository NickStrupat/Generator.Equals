﻿using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Generator.Equals.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator.Equals.Models;

internal sealed class EqualityTypeModelTransformer
{
    private readonly GeneratorAttributeSyntaxContext _context;

    public EqualityTypeModelTransformer(GeneratorAttributeSyntaxContext context)
    {
        _context = context;
    }

    public EqualityTypeModel? Transform(CancellationToken token = default)
    {
        if (token.IsCancellationRequested)
        {
            return null;
        }

        var attributesMetadata = AttributesMetadata.Instance;

        var equatableAttributeData = _context.Attributes.SingleOrDefault();
        if (equatableAttributeData is null || !attributesMetadata.Equatable.Equals(equatableAttributeData.AttributeClass))
        {
            // TODO: Report diagnostic
            // throw new Exception("Expected exactly one EquatableAttribute.");
            return null;
        }

        if (token.IsCancellationRequested)
        {
            return null;
        }

        var explicitMode = equatableAttributeData.GetNamedArgumentValue("Explicit") is true;
        var ignoreInheritedMembers = equatableAttributeData.GetNamedArgumentValue("IgnoreInheritedMembers") is true;

        if (_context.TargetSymbol is not ITypeSymbol symbol)
        {
            // TODO: Report diagnostic
            // throw new Exception("Expected a type symbol.");
            return null;
        }

        if (token.IsCancellationRequested)
        {
            return null;
        }

        var baseTypeName = symbol.BaseType?.ToFQF();

        var typeName = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var fullname = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        if (token.IsCancellationRequested)
        {
            return null;
        }

        var containingSymbols = GetContainingSymbols(symbol, includeSelf: symbol.IsRecord);
        if (token.IsCancellationRequested)
        {
            return null;
        }

        var bems = EqualityMemberModelTransformer.BuildEqualityModels(symbol, attributesMetadata, explicitMode);

        var model = new EqualityTypeModel
        {
            TypeName = typeName,
            ContainingSymbols = containingSymbols,
            AttributesMetadata = attributesMetadata,
            ExplicitMode = explicitMode,
            IgnoreInheritedMembers = ignoreInheritedMembers,
            BuildEqualityModels = bems,
            IsSealed = symbol.IsSealed,
            BaseTypeName = baseTypeName,
            Fullname = fullname,
            SyntaxKind = _context.TargetNode.Kind()
        };

        if (model.SyntaxKind is not (
            SyntaxKind.StructDeclaration
            or SyntaxKind.RecordStructDeclaration
            or SyntaxKind.RecordDeclaration
            or SyntaxKind.ClassDeclaration
            )
           )
        {
            // Todo: Report diagnostic
            // throw new Exception("Expected a struct, record struct, record, or class declaration syntax.");
            return null;
        }

        return model;
    }

    public static ImmutableArray<ContainingSymbol> GetContainingSymbols(ISymbol symbol, bool includeSelf = false)
    {
        var parentSymbols = symbol
            .GetParentSymbols(includeSelf)
            .TakeUntilAfterNamespace();

        return parentSymbols.Select(x =>
            {
                if (x.IsNamespace)
                {
                    var format = SymbolDisplayFormat.FullyQualifiedFormat
                        .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);

                    var namespaceName = x.ToDisplayString(format);

                    return (ContainingSymbol)new NamespaceContainingSymbol
                    {
                        Name = namespaceName
                    };
                }

                var typeDeclarationSyntax = x.DeclaringSyntaxReferences
                    .Select(x => x.GetSyntax())
                    .OfType<TypeDeclarationSyntax>()
                    .First();

                var typeName = x.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

                return (ContainingSymbol)new TypeContainingSymbol
                {
                    Name = typeName,
                    Kind = typeDeclarationSyntax.Kind()
                };
            })
            .ToImmutableArray();
    }
}