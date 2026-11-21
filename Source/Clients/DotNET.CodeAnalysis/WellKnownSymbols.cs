// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis;

/// <summary>
/// Shares symbol resolution and structural recognition across analyzers of one compilation.
/// </summary>
/// <param name="compilation">The owning compilation.</param>
internal sealed class WellKnownSymbols(Compilation compilation)
{
    static readonly CompilationCache<WellKnownSymbols> _cache = new();
    readonly ConcurrentDictionary<string, Lazy<INamedTypeSymbol?>> _symbols = new();
    readonly ConcurrentDictionary<ITypeSymbol, ImmutableHashSet<INamedTypeSymbol>> _interfaces = new(SymbolEqualityComparer.Default);
    readonly ConcurrentDictionary<INamedTypeSymbol, bool> _selfReducers = new(SymbolEqualityComparer.Default);

    /// <summary>
    /// Gets the shared compilation state without retaining completed compilations.
    /// </summary>
    /// <param name="compilation">The owning compilation.</param>
    /// <returns>The shared symbols.</returns>
    internal static WellKnownSymbols For(Compilation compilation) =>
        _cache.For(compilation, (key, _) => new(key), CancellationToken.None);

    /// <summary>
    /// Resolves a metadata name once, including missing or ambiguous types.
    /// </summary>
    /// <param name="name">The metadata name.</param>
    /// <returns>The resolved symbol, if unambiguous.</returns>
    internal INamedTypeSymbol? Get(string name) =>
        _symbols.GetOrAdd(name, key => new(() => compilation.GetTypeByMetadataName(key))).Value;

    /// <summary>
    /// Checks an interface against a type's cached original interface definitions.
    /// </summary>
    /// <param name="type">The candidate type.</param>
    /// <param name="name">The interface metadata name.</param>
    /// <returns>Whether the type implements the interface.</returns>
    internal bool Implements(ITypeSymbol type, string name) =>
        Get(name) is { } contract && _interfaces.GetOrAdd(type, candidate =>
            candidate.AllInterfaces.Select(item => item.OriginalDefinition).ToImmutableHashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default)).Contains(contract);

    /// <summary>
    /// Recognizes a self reducer once per type across analyzer callbacks.
    /// </summary>
    /// <param name="type">The candidate model.</param>
    /// <returns>Whether the type has a valid static transition.</returns>
    internal bool IsSelf(INamedTypeSymbol type) => _selfReducers.GetOrAdd(type, candidate =>
        candidate is { TypeKind: TypeKind.Class, IsAbstract: false, IsGenericType: false } &&
        SelfReducerPolicy.ReduceMethods(candidate).Any(method =>
            SelfReducerPolicy.IsValid(method, candidate, compilation) || SelfReducerPolicy.IsValidBatch(method, candidate, compilation)) &&
        SelfReducerPolicy.CanBeSelf(candidate, compilation));
}
