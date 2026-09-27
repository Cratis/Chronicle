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
public class ExplicitArtifacts
{
#if NET8_0
    readonly object _lock = new();
#else
    readonly Lock _lock = new();
#endif
    readonly List<IExplicitProjection> _projections = [];
    readonly List<(ReactorId Id, Action<IReactorBuilder> Define)> _reactors = [];

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
    /// <param name="id">Optional <see cref="ProjectionId"/>. Defaults to the full name of <typeparamref name="TReadModel"/>.</param>
    /// <returns>The same <see cref="ExplicitArtifacts"/> for continuation.</returns>
    /// <remarks>
    /// This behaves as <see cref="IProjections.Register{TReadModel}(Action{IProjectionBuilderFor{TReadModel}}, ProjectionId?)"/>
    /// does, for every event store the client creates.
    /// </remarks>
    public ExplicitArtifacts RegisterProjection<TReadModel>(Action<IProjectionBuilderFor<TReadModel>> define, ProjectionId? id = null)
    {
        ArgumentNullException.ThrowIfNull(define);
        return Add(new DeclarativeExplicitProjection<TReadModel>(define, id ?? new ProjectionId(typeof(TReadModel).FullName!)));
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
