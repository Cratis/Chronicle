// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Wraps an <see cref="IClientArtifactsProvider"/> and delegates to a mutable
/// reference so that the static silo can serve artifacts from the current test fixture.
/// The instance is stored on <see cref="Instance"/> so test fixtures can swap the
/// active provider without resolving from DI (the host and silo have separate
/// service providers).
/// </summary>
/// <param name="initial">The initial <see cref="IClientArtifactsProvider"/> to delegate to.</param>
internal class DelegatingClientArtifactsProvider(IClientArtifactsProvider initial) : IClientArtifactsProvider
{
    static readonly object _lock = new();
    volatile IClientArtifactsProvider _current = initial;

    /// <inheritdoc/>
    public IEnumerable<Type> EventTypes => _current.EventTypes;

    /// <inheritdoc/>
    public IEnumerable<Type> Projections => _current.Projections;

    /// <inheritdoc/>
    public IEnumerable<Type> ModelBoundProjections => _current.ModelBoundProjections;

    /// <inheritdoc/>
    public IEnumerable<Type> Reactors => _current.Reactors;

    /// <inheritdoc/>
    public IEnumerable<Type> Reducers => _current.Reducers;

    /// <inheritdoc/>
    public IEnumerable<Type> ReactorMiddlewares => _current.ReactorMiddlewares;

    /// <inheritdoc/>
    public IEnumerable<Type> ComplianceForTypesProviders => _current.ComplianceForTypesProviders;

    /// <inheritdoc/>
    public IEnumerable<Type> ComplianceForPropertiesProviders => _current.ComplianceForPropertiesProviders;

    /// <inheritdoc/>
    public IEnumerable<Type> AdditionalEventInformationProviders => _current.AdditionalEventInformationProviders;

    /// <inheritdoc/>
    public IEnumerable<Type> ConstraintTypes => _current.ConstraintTypes;

    /// <inheritdoc/>
    public IEnumerable<Type> UniqueConstraints => _current.UniqueConstraints;

    /// <inheritdoc/>
    public IEnumerable<Type> UniqueEventTypeConstraints => _current.UniqueEventTypeConstraints;

    /// <inheritdoc/>
    public IEnumerable<Type> RemoveConstraintEventTypes => _current.RemoveConstraintEventTypes;

    /// <inheritdoc/>
    public IEnumerable<Type> EventTypeMigrators => _current.EventTypeMigrators;

    /// <inheritdoc/>
    public IEnumerable<Type> EventSeeders => _current.EventSeeders;

    /// <summary>
    /// Gets or sets the singleton instance created during factory construction.
    /// </summary>
    internal static DelegatingClientArtifactsProvider? Instance { get; set; }

    /// <summary>
    /// Gets the existing singleton instance or creates it with the provided initial provider.
    /// </summary>
    /// <param name="initial">The initial <see cref="IClientArtifactsProvider"/>.</param>
    /// <returns>The shared <see cref="DelegatingClientArtifactsProvider"/> instance.</returns>
    internal static DelegatingClientArtifactsProvider GetOrCreate(IClientArtifactsProvider initial)
    {
        lock (_lock)
        {
            Instance ??= new(initial);
            return Instance;
        }
    }

    /// <summary>
    /// Points the delegate at a new provider (typically the current test fixture).
    /// </summary>
    /// <param name="provider">The <see cref="IClientArtifactsProvider"/> to delegate to.</param>
    internal void SetCurrent(IClientArtifactsProvider provider) => _current = provider;
}
