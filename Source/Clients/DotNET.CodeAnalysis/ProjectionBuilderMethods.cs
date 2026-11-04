// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Chronicle.CodeAnalysis;

/// <summary>
/// Finds a conservative set of event-taking method names without binding call sites or loading referenced type symbols.
/// </summary>
internal static class ProjectionBuilderMethods
{
    /// <summary>
    /// Collects every method name with a TEvent type parameter; declaring builder types are checked after binding.
    /// </summary>
    /// <param name="compilation">The compilation containing the builder surface.</param>
    /// <param name="cancellationToken">The analysis cancellation token.</param>
    /// <returns>The names that can accept an event type argument.</returns>
    internal static ImmutableHashSet<string> Names(Compilation compilation, CancellationToken cancellationToken)
    {
        var names = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        AddSourceNames(compilation, names, cancellationToken);
        foreach (var reference in compilation.References)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddReferenceNames(reference, names, cancellationToken);
        }
        return names.ToImmutable();
    }

    /// <summary>
    /// Checks the declaring type, not bound generic arguments, for a builder identity.
    /// </summary>
    /// <param name="type">The declaring type.</param>
    /// <returns>Whether the type is a recognized builder.</returns>
    internal static bool IsBuilder(INamedTypeSymbol type)
    {
        var name = type.OriginalDefinition.ToDisplayString();
        return name.Contains("IProjectionBuilderFor") ||
               name.Contains("IProjectionBuilder") ||
               name.Contains("IFromBuilder") ||
               name.Contains("IJoinBuilder") ||
               name.Contains("IChildrenBuilder");
    }

    static void AddReferenceNames(MetadataReference reference, ImmutableHashSet<string>.Builder names, CancellationToken cancellationToken)
    {
        if (reference is CompilationReference source) AddSourceNames(source.Compilation, names, cancellationToken);
        if (reference is not PortableExecutableReference portable) return;
        foreach (var module in Modules(portable.GetMetadata())) AddMetadataNames(module, names, cancellationToken);
    }

    static ImmutableArray<ModuleMetadata> Modules(Metadata metadata) => metadata switch
    {
        AssemblyMetadata assembly => assembly.GetModules(),
        ModuleMetadata module => [module],
        _ => []
    };

    static void AddSourceNames(Compilation compilation, ImmutableHashSet<string>.Builder names, CancellationToken cancellationToken)
    {
        foreach (var tree in compilation.SyntaxTrees)
        {
            foreach (var node in tree.GetRoot(cancellationToken).DescendantNodes())
            {
                if (node is MethodDeclarationSyntax method && HasEventParameter(method.TypeParameterList)) names.Add(method.Identifier.ValueText);
                if (node is LocalFunctionStatementSyntax local && HasEventParameter(local.TypeParameterList)) names.Add(local.Identifier.ValueText);
            }
        }
    }

    static bool HasEventParameter(TypeParameterListSyntax? parameters) =>
        parameters?.Parameters.Any(parameter => parameter.Identifier.ValueText == "TEvent") == true;

    static void AddMetadataNames(ModuleMetadata module, ImmutableHashSet<string>.Builder names, CancellationToken cancellationToken)
    {
        var reader = module.GetMetadataReader();
        for (var row = 1; row <= reader.GetTableRowCount(TableIndex.GenericParam); row++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var parameter = reader.GetGenericParameter(MetadataTokens.GenericParameterHandle(row));
            if (parameter.Parent.Kind != HandleKind.MethodDefinition || !reader.StringComparer.Equals(parameter.Name, "TEvent")) continue;
            var method = reader.GetMethodDefinition((MethodDefinitionHandle)parameter.Parent);
            names.Add(reader.GetString(method.Name));
        }
    }
}
