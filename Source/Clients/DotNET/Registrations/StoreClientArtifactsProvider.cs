// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// Delegates to native manual discovery until an explicit store revision captures the original options snapshot.
/// </summary>
/// <param name="native">The native provider, which may itself delegate to fixture-specific discovery.</param>
/// <param name="snapshot">The options recipes captured at store construction.</param>
internal sealed class StoreClientArtifactsProvider(IClientArtifactsProvider native, ExplicitArtifactsSnapshot snapshot) : IClientArtifactsProvider
{
    IClientArtifactsProvider? _catalog;

    /// <inheritdoc/>
    public IEnumerable<Type> EventTypes => Current.EventTypes;

    /// <inheritdoc/>
    public IEnumerable<Type> Projections => Current.Projections;

    /// <inheritdoc/>
    public IEnumerable<Type> ModelBoundProjections => Current.ModelBoundProjections;

    /// <inheritdoc/>
    public IEnumerable<Type> Reactors => Current.Reactors;

    /// <inheritdoc/>
    public IEnumerable<Type> ReadModelReactors => Current.ReadModelReactors;

    /// <inheritdoc/>
    public IEnumerable<Type> Reducers => Current.Reducers;

    /// <inheritdoc/>
    public IEnumerable<Type> ReactorMiddlewares => Current.ReactorMiddlewares;

    /// <inheritdoc/>
    public IEnumerable<Type> ComplianceForTypesProviders => Current.ComplianceForTypesProviders;

    /// <inheritdoc/>
    public IEnumerable<Type> ComplianceForPropertiesProviders => Current.ComplianceForPropertiesProviders;

    /// <inheritdoc/>
    public IEnumerable<Type> SecurityForTypesProviders => Current.SecurityForTypesProviders;

    /// <inheritdoc/>
    public IEnumerable<Type> SecurityForPropertiesProviders => Current.SecurityForPropertiesProviders;

    /// <inheritdoc/>
    public IEnumerable<Type> AdditionalEventInformationProviders => Current.AdditionalEventInformationProviders;

    /// <inheritdoc/>
    public IEnumerable<Type> ConstraintTypes => Current.ConstraintTypes;

    /// <inheritdoc/>
    public IEnumerable<Type> UniqueConstraints => Current.UniqueConstraints;

    /// <inheritdoc/>
    public IEnumerable<Type> UniqueEventTypeConstraints => Current.UniqueEventTypeConstraints;

    /// <inheritdoc/>
    public IEnumerable<Type> RemoveConstraintEventTypes => Current.RemoveConstraintEventTypes;

    /// <inheritdoc/>
    public IEnumerable<Type> ClosesStreamEventTypes => Current.ClosesStreamEventTypes;

    /// <inheritdoc/>
    public IEnumerable<Type> EventTypeMigrators => Current.EventTypeMigrators;

    /// <inheritdoc/>
    public IEnumerable<Type> EventSeeders => Current.EventSeeders;

    /// <inheritdoc/>
    public IEnumerable<Type> EventSources => Current.EventSources;

    /// <inheritdoc/>
    public IEnumerable<Type> Triggers => Current.Triggers;

    /// <inheritdoc/>
    public IEnumerable<Type> OccurrenceHandlers => Current.OccurrenceHandlers;

    /// <summary>
    /// Gets the currently captured immutable catalog, or the untouched native manual provider.
    /// </summary>
    internal IClientArtifactsProvider Current => Volatile.Read(ref _catalog) ?? native;

    /// <summary>
    /// Copies fresh native discovery while retaining the store's original immutable options recipes.
    /// </summary>
    internal void Prepare() => Volatile.Write(ref _catalog, snapshot.ApplyTo(native));
}
