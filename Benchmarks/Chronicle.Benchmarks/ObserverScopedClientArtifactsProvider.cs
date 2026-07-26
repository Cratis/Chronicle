// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Represents an <see cref="IClientArtifactsProvider"/> that narrows the discovered observers down to an explicit set.
/// </summary>
/// <remarks>
/// Waiting for observer completion after an append waits for every observer registered for the event sequence in the
/// event store and namespace, regardless of which event types the individual observer subscribes to. A benchmark that
/// measures a single observer in isolation therefore has to make sure that observer is the only one registered.
/// </remarks>
/// <param name="observers">The observer types to keep. For model-bound projections this is the read model type.</param>
/// <exception cref="ArgumentException">Thrown when one of the observer types was not discovered as an observer.</exception>
public class ObserverScopedClientArtifactsProvider(params Type[] observers) : IClientArtifactsProvider
{
    readonly DefaultClientArtifactsProvider _inner = DefaultClientArtifactsProvider.Default;
    readonly HashSet<Type> _observers = EnsureDiscovered(observers);

    /// <inheritdoc/>
    public IEnumerable<Type> EventTypes => _inner.EventTypes;

    /// <inheritdoc/>
    public IEnumerable<Type> Projections => Scoped(_inner.Projections);

    /// <inheritdoc/>
    public IEnumerable<Type> ModelBoundProjections => Scoped(_inner.ModelBoundProjections);

    /// <inheritdoc/>
    public IEnumerable<Type> Reactors => Scoped(_inner.Reactors);

    /// <inheritdoc/>
    public IEnumerable<Type> ReadModelReactors => Scoped(_inner.ReadModelReactors);

    /// <inheritdoc/>
    public IEnumerable<Type> Reducers => Scoped(_inner.Reducers);

    /// <inheritdoc/>
    public IEnumerable<Type> ReactorMiddlewares => _inner.ReactorMiddlewares;

    /// <inheritdoc/>
    public IEnumerable<Type> ComplianceForTypesProviders => _inner.ComplianceForTypesProviders;

    /// <inheritdoc/>
    public IEnumerable<Type> ComplianceForPropertiesProviders => _inner.ComplianceForPropertiesProviders;

    /// <inheritdoc/>
    public IEnumerable<Type> AdditionalEventInformationProviders => _inner.AdditionalEventInformationProviders;

    /// <inheritdoc/>
    public IEnumerable<Type> ConstraintTypes => _inner.ConstraintTypes;

    /// <inheritdoc/>
    public IEnumerable<Type> UniqueConstraints => _inner.UniqueConstraints;

    /// <inheritdoc/>
    public IEnumerable<Type> UniqueEventTypeConstraints => _inner.UniqueEventTypeConstraints;

    /// <inheritdoc/>
    public IEnumerable<Type> RemoveConstraintEventTypes => _inner.RemoveConstraintEventTypes;

    /// <inheritdoc/>
    public IEnumerable<Type> EventTypeMigrators => _inner.EventTypeMigrators;

    /// <inheritdoc/>
    public IEnumerable<Type> EventSeeders => _inner.EventSeeders;

    static HashSet<Type> EnsureDiscovered(Type[] observers)
    {
        // An observer type that is not discovered scopes down to nothing, and an event sequence without observers
        // reports completion immediately - which would leave the benchmark measuring the append alone.
        var inner = DefaultClientArtifactsProvider.Default;
        var discovered = new HashSet<Type>(
        [
            .. inner.Projections,
            .. inner.ModelBoundProjections,
            .. inner.Reactors,
            .. inner.ReadModelReactors,
            .. inner.Reducers
        ]);

        var missing = observers.Where(observer => !discovered.Contains(observer)).ToArray();
        if (missing.Length > 0)
        {
            throw new ArgumentException(
                $"The following types were not discovered as observers: {string.Join(", ", missing.Select(type => type.Name))}.",
                nameof(observers));
        }

        return [.. observers];
    }

    IEnumerable<Type> Scoped(IEnumerable<Type> types) => types.Where(_observers.Contains);
}
