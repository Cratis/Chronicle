// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// Represents artifacts registered explicitly with the client while it is being configured, rather than found by
/// discovery - for every event store the client creates.
/// </summary>
/// <remarks>
/// <para>
/// Reach this through <see cref="ChronicleOptions.ExplicitArtifacts"/> when the registrations are known before the
/// client connects, typically from hosting or dependency injection setup. Each event store the client creates from then
/// on - whatever its namespace - receives them, builds them when it discovers its artifacts, and includes them in its
/// normal registration pass and in every re-registration after a reconnect, exactly as it does its discovered artifacts.
/// </para>
/// <para>
/// A registration made after an event store has been created does not reach that event store. To register with an
/// event store that already exists - connected or not - use <see cref="IProjections.Register{TReadModel}(Action{IProjectionBuilderFor{TReadModel}}, ProjectionId?)"/>,
/// <see cref="IProjections.Register{TReadModel}()"/> and <see cref="IReactors.Register(ReactorId, Action{IReactorBuilder})"/>
/// on it instead; those register with Chronicle immediately when connected.
/// </para>
/// <para>
/// Registrations are built during discovery, so they only take effect for event stores that discover their artifacts -
/// the default, governed by <see cref="ChronicleOptions.AutoDiscoverAndRegister"/>. A projection that cannot be built is
/// reported through <see cref="IEventStore.Registration"/> like a discovered one; a reactor that cannot be defined fails
/// discovery.
/// </para>
/// </remarks>
public partial class ExplicitArtifacts
{
#if NET8_0
    readonly object _lock = new();
#else
    readonly Lock _lock = new();
#endif
    readonly List<IExplicitProjection> _projections = [];
    readonly List<(ReactorId Id, Action<IReactorBuilder> Define)> _reactors = [];
    readonly List<EventTypeRegistration> _eventTypes = [];
    readonly List<ExplicitArtifactRegistration> _types = [];
    int _ordinal;

    /// <summary>
    /// Gets the explicitly registered projections.
    /// </summary>
    internal IEnumerable<IExplicitProjection> Projections
    {
        get
        {
            lock (_lock)
            {
                return [.. _projections];
            }
        }
    }

    /// <summary>
    /// Gets the explicitly registered reactors.
    /// </summary>
    internal IEnumerable<(ReactorId Id, Action<IReactorBuilder> Define)> Reactors
    {
        get
        {
            lock (_lock)
            {
                return [.. _reactors];
            }
        }
    }

    /// <summary>
    /// Register a read model with a projection defined through the same builder an <see cref="IProjectionFor{TReadModel}"/> uses.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model the projection maintains.</typeparam>
    /// <param name="define">Callback that defines the projection.</param>
    /// <param name="id">Optional <see cref="ProjectionId"/>. When supplied, takes precedence over <see cref="ProjectionAttribute"/>. Otherwise uses the read model's non-empty attribute ID or its fully qualified type name.</param>
    /// <returns>The same <see cref="ExplicitArtifacts"/> for continuation.</returns>
    /// <remarks>
    /// This behaves as <see cref="IProjections.Register{TReadModel}(Action{IProjectionBuilderFor{TReadModel}}, ProjectionId?)"/>
    /// does, for every event store the client creates.
    /// </remarks>
    public ExplicitArtifacts RegisterProjection<TReadModel>(Action<IProjectionBuilderFor<TReadModel>> define, ProjectionId? id = null)
    {
        ArgumentNullException.ThrowIfNull(define);
        return Add(new DeclarativeExplicitProjection<TReadModel>(define, id ?? ArtifactIdentity.ModelProjectionIdFor(typeof(TReadModel))));
    }

    /// <summary>
    /// Register a read model whose projection is declared by its model-bound attributes.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model carrying the model-bound projection attributes.</typeparam>
    /// <returns>The same <see cref="ExplicitArtifacts"/> for continuation.</returns>
    /// <remarks>
    /// This behaves as <see cref="IProjections.Register{TReadModel}()"/> does, for every event store the client creates.
    /// A read model that discovery also finds is registered once.
    /// </remarks>
    public ExplicitArtifacts RegisterReadModel<TReadModel>() => Add(new ModelBoundExplicitProjection(typeof(TReadModel)));

    /// <summary>
    /// Register a reactor declared fluently.
    /// </summary>
    /// <param name="id">The stable reactor identifier.</param>
    /// <param name="define">Declares the reactor's handlers on an <see cref="IReactorBuilder"/>.</param>
    /// <returns>The same <see cref="ExplicitArtifacts"/> for continuation.</returns>
    /// <remarks>
    /// This behaves as <see cref="IReactors.Register(ReactorId, Action{IReactorBuilder})"/> does, for every event store
    /// the client creates. The definition callback runs once per event store, the first time it discovers its artifacts -
    /// so an all-events subscription covers the event types known at that point.
    /// </remarks>
    public ExplicitArtifacts RegisterReactor(ReactorId id, Action<IReactorBuilder> define)
    {
        ArgumentNullException.ThrowIfNull(define);
        lock (_lock)
        {
            _reactors.Add((id, define));
        }

        return this;
    }

    /// <summary>
    /// Applies projection recipes to a store before discovery.
    /// </summary>
    /// <param name="projections">The store's projections.</param>
    internal void ApplyTo(Projections.Projections projections)
    {
        foreach (var projection in Projections)
        {
            projections.Add(projection);
        }
    }

    /// <summary>
    /// Applies reactor recipes to a store before discovery.
    /// </summary>
    /// <param name="reactors">The store's reactors.</param>
    internal void ApplyTo(Reactors.Reactors reactors)
    {
        foreach (var (id, define) in Reactors)
        {
            reactors.Add(id, define);
        }
    }

    ExplicitArtifacts Add(IExplicitProjection projection)
    {
        lock (_lock)
        {
            _projections.RemoveAll(_ => _.ReadModelType == projection.ReadModelType);
            _projections.Add(projection);
        }

        return this;
    }
}
