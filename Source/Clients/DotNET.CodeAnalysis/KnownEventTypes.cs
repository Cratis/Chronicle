// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis;

/// <summary>
/// Collects the event types a compilation can see, the way the client knows its event types at runtime.
/// </summary>
/// <remarks>
/// The client expands an observed base type or interface to every known event type it covers. The analyzers do the
/// same with this set: the event types declared in the compilation, those declared in referenced assemblies that
/// themselves reference the assembly declaring the event type attribute, and the explicitly registered ones.
/// </remarks>
internal static class KnownEventTypes
{
    /// <summary>
    /// Collects the known event types of a compilation.
    /// </summary>
    /// <param name="compilation">The compilation.</param>
    /// <param name="evidence">The <see cref="ExplicitRegistrationEvidence"/> of the compilation.</param>
    /// <param name="cancellationToken">The analysis cancellation token.</param>
    /// <returns>The known event types.</returns>
    internal static ImmutableArray<ITypeSymbol> In(Compilation compilation, ExplicitRegistrationEvidence evidence, CancellationToken cancellationToken)
    {
        var eventTypes = ImmutableHashSet.CreateBuilder<ITypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var assembly in AssembliesDeclaringEvents(compilation))
        {
            cancellationToken.ThrowIfCancellationRequested();
            eventTypes.UnionWith(SelfReducerPolicy.Types(assembly.GlobalNamespace).Where(type => type.TypeKind == TypeKind.Class && WellKnownTypes.HasEventTypeAttribute(type)));
        }

        eventTypes.UnionWith(evidence.EventTypes);
        return [.. eventTypes];
    }

    static IEnumerable<IAssemblySymbol> AssembliesDeclaringEvents(Compilation compilation)
    {
        yield return compilation.Assembly;

        var attribute = compilation.GetTypeByMetadataName(WellKnownTypes.ClientEventTypeAttributeName)?.ContainingAssembly;
        if (attribute is null || SymbolEqualityComparer.Default.Equals(attribute, compilation.Assembly))
        {
            yield break;
        }

        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols.Where(assembly =>
            assembly.Modules.Any(module => module.ReferencedAssemblySymbols.Any(referenced => SymbolEqualityComparer.Default.Equals(referenced, attribute)))))
        {
            yield return assembly;
        }
    }
}
