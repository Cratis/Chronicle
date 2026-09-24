// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Kernel;

/// <summary>
/// Represents an implementation of <see cref="IKernelProjectionBuilder{TReadModel}"/> that lowers a system
/// projection declaration into the same <see cref="ProjectionDefinition"/> the projection engine runs for a
/// client-registered projection.
/// </summary>
/// <typeparam name="TReadModel">Type of the read model the projection materializes.</typeparam>
/// <param name="identifier">The <see cref="ProjectionId"/> of the projection.</param>
/// <param name="readModel">The <see cref="ReadModelIdentifier"/> the projection materializes into.</param>
/// <remarks>
/// Nothing here is a second projection engine - the declaration is lowered to a definition and handed to the
/// engine that already exists. What the surface adds is that the kernel can state a projection in C# against its
/// own read model types, with the owner, the identifier prefix and the non-replayable rule applied for it rather
/// than repeated at every declaration site.
/// </remarks>
public class KernelProjectionBuilder<TReadModel>(ProjectionId identifier, ReadModelIdentifier readModel)
    : IKernelProjectionBuilder<TReadModel>
    where TReadModel : class
{
    /// <summary>
    /// The identifier suffix distinguishing the globally scoped half of a <see cref="ProjectionScope.Both"/>
    /// declaration from its namespaced half.
    /// </summary>
    public const string GlobalIdentifierSuffix = ".global";

    readonly Dictionary<EventType, FromDefinition> _from = [];
    readonly Dictionary<PropertyPath, string> _fromEvery = [];
    readonly JsonObject _initialState = [];
    ProjectionScope _scope = ProjectionScope.Namespaced;
    EventSequenceId _eventSequenceId = EventSequenceId.Log;
    PropertyExpression _key = WellKnownExpressions.EventSourceId;
    bool _subscribesToAllEvents;

    /// <inheritdoc/>
    public IKernelProjectionBuilder<TReadModel> ScopedTo(ProjectionScope scope)
    {
        _scope = scope;
        return this;
    }

    /// <inheritdoc/>
    public IKernelProjectionBuilder<TReadModel> FromEventSequence(EventSequenceId eventSequenceId)
    {
        _eventSequenceId = eventSequenceId;
        return this;
    }

    /// <inheritdoc/>
    public IKernelProjectionBuilder<TReadModel> IdentifiedBy(PropertyExpression expression)
    {
        _key = expression;
        return this;
    }

    /// <inheritdoc/>
    public IKernelProjectionBuilder<TReadModel> IdentifiedByComposite(Action<IKernelProjectionCompositeKeyBuilder<TReadModel>> builderCallback)
    {
        var builder = new KernelProjectionCompositeKeyBuilder<TReadModel>();
        builderCallback(builder);
        _key = builder.Build();
        return this;
    }

    /// <inheritdoc/>
    public IKernelProjectionBuilder<TReadModel> From(EventType eventType, Action<IKernelProjectionFromBuilder<TReadModel>> builderCallback)
    {
        var builder = new KernelProjectionFromBuilder<TReadModel>();
        builderCallback(builder);
        _from[eventType] = new FromDefinition(builder.Properties, _key, null);
        return this;
    }

    /// <inheritdoc/>
    public IKernelProjectionBuilder<TReadModel> FromEvery(Action<IKernelProjectionFromBuilder<TReadModel>> builderCallback)
    {
        var builder = new KernelProjectionFromBuilder<TReadModel>();
        builderCallback(builder);

        foreach (var (path, expression) in builder.Properties)
        {
            _fromEvery[path] = expression;
        }

        _subscribesToAllEvents = true;
        return this;
    }

    /// <inheritdoc/>
    public IKernelProjectionBuilder<TReadModel> WithInitialValue<TProperty>(Expression<Func<TReadModel, TProperty>> propertyAccessor, TProperty value)
    {
        var path = KernelProjectionPropertyPathResolver.Resolve(propertyAccessor);
        _initialState[path.Path] = JsonSerializer.SerializeToNode(value);
        return this;
    }

    /// <summary>
    /// Build the <see cref="ProjectionDefinition"/> definitions the declaration lowers to.
    /// </summary>
    /// <returns>The built definitions - one, or two for <see cref="ProjectionScope.Both"/>.</returns>
    /// <remarks>
    /// <para>
    /// The owner and the rewindability are decided here rather than left to the declaration. A kernel projection
    /// that could be declared replayable would be a kernel projection someone can replay.
    /// </para>
    /// <para>
    /// <see cref="ProjectionScope.Both"/> lowers to two single-scope definitions rather than one definition the
    /// engine has to materialize twice. A projection is observed and materialized per namespace, so "both" is two
    /// observers by nature; making that explicit at declaration time keeps the engine unaware there is such a
    /// thing as a dual scope, and leaves each half an ordinary projection that can be inspected, reasoned about
    /// and diagnosed on its own.
    /// </para>
    /// </remarks>
    public IReadOnlyCollection<ProjectionDefinition> Build() => _scope switch
    {
        ProjectionScope.Both =>
        [
            BuildFor(identifier, ProjectionScope.Namespaced),
            BuildFor(new ProjectionId($"{identifier.Value}{GlobalIdentifierSuffix}"), ProjectionScope.Global)
        ],
        _ => [BuildFor(identifier, _scope)]
    };

    ProjectionDefinition BuildFor(ProjectionId projectionId, ProjectionScope scope) =>
        new(
            ProjectionOwner.Kernel,
            _eventSequenceId,
            projectionId,
            readModel,
            IsActive: true,
            IsRewindable: false,
            _initialState,
            _from,
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new FromEveryDefinition(_fromEvery, false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            LastUpdated: DateTimeOffset.UtcNow,
            SubscribesToAllEvents: _subscribesToAllEvents,
            Scope: scope);
}
