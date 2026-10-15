// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Registrations;

public partial class ExplicitArtifacts
{
    /// <summary>
    /// Registers an event type for every subsequently created store.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="configure">Optional supplied declaration facets; omitted facets inherit markers or defaults.</param>
    /// <returns>The same options object for continuation.</returns>
    public ExplicitArtifacts RegisterEventType<TEvent>(Action<IEventTypeRegistrationBuilder>? configure = null) =>
        RegisterEventType(typeof(TEvent), configure);

    /// <summary>
    /// Registers a CLR event type for every subsequently created store.
    /// </summary>
    /// <param name="eventType">The CLR event type.</param>
    /// <param name="configure">Optional supplied declaration facets; omitted facets inherit markers or defaults.</param>
    /// <returns>The same options object for continuation.</returns>
    public ExplicitArtifacts RegisterEventType(Type eventType, Action<IEventTypeRegistrationBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        var builder = new EventTypeRegistrationBuilder(eventType);
        configure?.Invoke(builder);
        lock (_lock)
        {
            _eventTypes.Add(builder.Build(++_ordinal));
        }

        return this;
    }

    /// <summary>
    /// Registers an instance-method reactor without requiring IReactor.
    /// </summary>
    /// <typeparam name="TReactor">The reactor type.</typeparam>
    /// <param name="id">Optional supplied identifier.</param>
    /// <returns>The same options object for continuation.</returns>
    public ExplicitArtifacts RegisterReactor<TReactor>(ReactorId? id = null) => RegisterReactor(typeof(TReactor), id);

    /// <summary>
    /// Registers an instance-method reactor by CLR type.
    /// </summary>
    /// <param name="reactorType">The reactor type.</param>
    /// <param name="id">Optional supplied identifier.</param>
    /// <returns>The same options object for continuation.</returns>
    public ExplicitArtifacts RegisterReactor(Type reactorType, ReactorId? id = null) => Add(reactorType, ExplicitArtifactKind.Reactor, id?.Value);

    /// <summary>
    /// Registers a reducer whose single read model is inferred from instance methods.
    /// </summary>
    /// <typeparam name="TReducer">The reducer type.</typeparam>
    /// <param name="id">Optional supplied identifier.</param>
    /// <returns>The same options object for continuation.</returns>
    public ExplicitArtifacts RegisterReducer<TReducer>(ReducerId? id = null) => RegisterReducer(typeof(TReducer), id);

    /// <summary>
    /// Registers a reducer by CLR type and inferred read model.
    /// </summary>
    /// <param name="reducerType">The reducer type.</param>
    /// <param name="id">Optional supplied identifier.</param>
    /// <returns>The same options object for continuation.</returns>
    public ExplicitArtifacts RegisterReducer(Type reducerType, ReducerId? id = null) => Add(reducerType, ExplicitArtifactKind.Reducer, id?.Value);

    /// <summary>
    /// Registers a constraint defined by Define(IConstraintBuilder).
    /// </summary>
    /// <typeparam name="TConstraint">The constraint definer type.</typeparam>
    /// <returns>The same options object for continuation.</returns>
    public ExplicitArtifacts RegisterConstraint<TConstraint>() => RegisterConstraint(typeof(TConstraint));

    /// <summary>
    /// Registers a constraint definer by CLR type.
    /// </summary>
    /// <param name="constraintType">The constraint definer type.</param>
    /// <returns>The same options object for continuation.</returns>
    public ExplicitArtifacts RegisterConstraint(Type constraintType) => Add(constraintType, ExplicitArtifactKind.Constraint);

    /// <summary>
    /// Registers an event source with its declared streams and optional name.
    /// </summary>
    /// <typeparam name="TSource">The event source type.</typeparam>
    /// <param name="name">Optional supplied event source name.</param>
    /// <returns>The same options object for continuation.</returns>
    public ExplicitArtifacts RegisterEventSource<TSource>(string? name = null) => RegisterEventSource(typeof(TSource), name);

    /// <summary>
    /// Registers an event source by CLR type.
    /// </summary>
    /// <param name="eventSourceType">The event source type.</param>
    /// <param name="name">Optional supplied event source name.</param>
    /// <returns>The same options object for continuation.</returns>
    public ExplicitArtifacts RegisterEventSource(Type eventSourceType, string? name = null) => Add(eventSourceType, ExplicitArtifactKind.EventSource, name);

    /// <summary>
    /// Registers an event migration for every subsequently created store.
    /// </summary>
    /// <typeparam name="TMigration">The migration type.</typeparam>
    /// <returns>The same options object for continuation.</returns>
    public ExplicitArtifacts RegisterMigration<TMigration>() => RegisterMigration(typeof(TMigration));

    /// <summary>
    /// Registers an event migration by CLR type.
    /// </summary>
    /// <param name="migrationType">The migration type.</param>
    /// <returns>The same options object for continuation.</returns>
    public ExplicitArtifacts RegisterMigration(Type migrationType) => Add(migrationType, ExplicitArtifactKind.Migration);

    /// <summary>
    /// Captures all recipes atomically for one store's lifetime.
    /// </summary>
    /// <returns>The immutable snapshot.</returns>
    internal ExplicitArtifactsSnapshot CreateSnapshot()
    {
        lock (_lock)
        {
            return new([.. _eventTypes], [.. _types], [.. _projections], [.. _reactors]);
        }
    }

    ExplicitArtifacts Add(Type type, ExplicitArtifactKind kind, string? identity = null)
    {
        ArgumentNullException.ThrowIfNull(type);
        lock (_lock)
        {
            var ordinal = ++_ordinal;
            _types.Add(new(type, kind, identity is null ? null : new(identity, $"explicit registration #{ordinal}")));
        }

        return this;
    }
}
