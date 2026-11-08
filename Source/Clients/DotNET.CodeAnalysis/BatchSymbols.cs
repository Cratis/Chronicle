// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis;

/// <summary>
/// Holds the symbols the batch method analyzers compare against, resolved once per compilation.
/// </summary>
/// <remarks>
/// Resolving a type by metadata name for every symbol or operation an analyzer sees is what makes an analyzer slow.
/// The analyzers create one of these when a compilation starts, and register nothing when the compilation cannot see
/// EventBatch, since then no batch method can exist.
/// </remarks>
internal sealed class BatchSymbols
{
    readonly Lazy<ExplicitRegistrationEvidence> _evidence;
    readonly Lazy<ImmutableArray<ITypeSymbol>> _knownEventTypes;

    BatchSymbols(Compilation compilation, INamedTypeSymbol eventBatch, CancellationToken cancellationToken)
    {
        Compilation = compilation;
        EventBatch = eventBatch;
        BatchContext = compilation.GetTypeByMetadataName(WellKnownTypes.BatchContextName);
        EventContext = compilation.GetTypeByMetadataName(WellKnownTypes.EventContextName);
        Observes = compilation.GetTypeByMetadataName(WellKnownTypes.ObservesAttributeName);
        ReducerFor = compilation.GetTypeByMetadataName(WellKnownTypes.IReducerForName);
        Task = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        TaskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        CancellationToken = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
        _evidence = new(() => ExplicitRegistrationEvidence.For(compilation, cancellationToken));
        _knownEventTypes = new(() => CodeAnalysis.KnownEventTypes.In(compilation, Evidence, cancellationToken));
    }

    /// <summary>
    /// Gets the compilation the symbols belong to.
    /// </summary>
    internal Compilation Compilation { get; }

    /// <summary>
    /// Gets the EventBatch type.
    /// </summary>
    internal INamedTypeSymbol EventBatch { get; }

    /// <summary>
    /// Gets the BatchContext type, if the compilation can see it.
    /// </summary>
    internal INamedTypeSymbol? BatchContext { get; }

    /// <summary>
    /// Gets the EventContext type, if the compilation can see it.
    /// </summary>
    internal INamedTypeSymbol? EventContext { get; }

    /// <summary>
    /// Gets the generic Observes attribute, if the compilation can see it.
    /// </summary>
    internal INamedTypeSymbol? Observes { get; }

    /// <summary>
    /// Gets the generic IReducerFor interface, if the compilation can see it.
    /// </summary>
    internal INamedTypeSymbol? ReducerFor { get; }

    /// <summary>
    /// Gets <see cref="System.Threading.Tasks.Task"/>.
    /// </summary>
    internal INamedTypeSymbol? Task { get; }

    /// <summary>
    /// Gets the generic Task.
    /// </summary>
    internal INamedTypeSymbol? TaskOfT { get; }

    /// <summary>
    /// Gets <see cref="System.Threading.CancellationToken"/>.
    /// </summary>
    internal INamedTypeSymbol? CancellationToken { get; }

    /// <summary>
    /// Gets the explicit registrations of the compilation, built the first time they are needed.
    /// </summary>
    internal ExplicitRegistrationEvidence Evidence => _evidence.Value;

    /// <summary>
    /// Gets the event types the compilation and the assemblies it references declare, built the first time they are needed.
    /// </summary>
    internal ImmutableArray<ITypeSymbol> KnownEventTypes => _knownEventTypes.Value;

    /// <summary>
    /// Resolves the symbols for a compilation.
    /// </summary>
    /// <param name="compilation">The compilation.</param>
    /// <param name="cancellationToken">The analysis cancellation token.</param>
    /// <returns>The symbols, or null when the compilation cannot see EventBatch.</returns>
    internal static BatchSymbols? For(Compilation compilation, CancellationToken cancellationToken)
    {
        var eventBatch = compilation.GetTypeByMetadataName(WellKnownTypes.EventBatchName);
        return eventBatch is null ? null : new(compilation, eventBatch, cancellationToken);
    }

    /// <summary>
    /// Gets whether a type is one of the resolved symbols.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="symbol">The resolved symbol.</param>
    /// <returns>True when it is.</returns>
    internal static bool Is(ITypeSymbol? type, INamedTypeSymbol? symbol) =>
        symbol is not null && SymbolEqualityComparer.Default.Equals(type, symbol);

    /// <summary>
    /// Gets whether a parameter type can carry events, the way the client tells from its known event types.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>True when it is an event type, or a base type or interface of a known event type.</returns>
    internal bool IsEventType(ITypeSymbol type) =>
        Evidence.IsEventType(type, Compilation) ||
        (type is { SpecialType: not SpecialType.System_Object, IsSealed: false, TypeKind: TypeKind.Class or TypeKind.Interface } &&
         KnownEventTypes.Any(eventType => Compilation.ClassifyCommonConversion(eventType, type) is { IsImplicit: true, IsUserDefined: false, IsReference: true }));

    /// <summary>
    /// Gets whether a type is the resolved generic Task of another type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="result">The result type.</param>
    /// <returns>True when it is.</returns>
    internal bool IsTaskOf(ITypeSymbol type, ITypeSymbol result) =>
        type is INamedTypeSymbol { IsGenericType: true } task &&
        Is(task.OriginalDefinition, TaskOfT) &&
        SymbolEqualityComparer.Default.Equals(task.TypeArguments[0], result);
}
