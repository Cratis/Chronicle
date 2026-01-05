// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.Definitions;

/// <summary>
/// Represents the definition of a projection.
/// </summary>
public class ProjectionDefinition
{
    /// <summary>
    /// Gets or sets the <see cref="ProjectionOwner">Owner</see> of the projection.
    /// </summary>
    public ProjectionOwner Owner { get; set; } = ProjectionOwner.None;

    /// <summary>
    /// Gets or sets the <see cref="EventSequenceId">Event sequence identifier</see> the projection projects from.
    /// </summary>
    public EventSequenceId EventSequenceId { get; set; } = EventSequenceId.Unspecified;

    /// <summary>
    /// Gets or sets the <see cref="ProjectionId">Identifier</see> of the projection.
    /// </summary>
    public ProjectionId Identifier { get; set; } = ProjectionId.Unspecified;

    /// <summary>
    /// Gets or sets the target read model.
    /// </summary>
    public ReadModelIdentifier ReadModel { get; set; } = ReadModelIdentifier.NotSet;

    /// <summary>
    /// Gets or sets whether or not the projection is an actively observing projection.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets whether or not the projection is rewindable.
    /// </summary>
    public bool IsRewindable { get; set; }

    /// <summary>
    /// Gets or sets the initial state to use for new instances of the model.
    /// </summary>
    public BsonDocument InitialModelState { get; set; } = new BsonDocument();

    /// <summary>
    /// Gets or sets all the <see cref="FromDefinition"/> for string representation of <see cref="EventType">event types</see>.
    /// </summary>
    public IDictionary<string, FromDefinition> From { get; set; } = new Dictionary<string, FromDefinition>();

    /// <summary>
    /// Gets or sets all the <see cref="JoinDefinition"/> for <see cref="EventType">event types</see>.
    /// </summary>
    public IDictionary<string, JoinDefinition> Join { get; set; } = new Dictionary<string, JoinDefinition>();

    /// <summary>
    /// Gets or sets all the <see cref="ChildrenDefinition"/> for properties on model.
    /// </summary>
    public IDictionary<string, ChildrenDefinition> Children { get; set; } = new Dictionary<string, ChildrenDefinition>();

    /// <summary>
    /// Gets or sets all the <see cref="Definitions.FromDerivatives"/> for an event type used as a base type in a From statement.
    /// </summary>
    public IEnumerable<FromDerivatives> FromDerivatives { get; set; } = [];

    /// <summary>
    /// Gets or sets the full <see cref="FromEveryDefinition"/>.
    /// </summary>
    public FromEveryDefinition FromEvery { get; set; } = new FromEveryDefinition();

    /// <summary>
    /// Gets or sets all the <see cref="RemovedWithDefinition"/> for string representation of <see cref="EventType">event types</see>.
    /// </summary>
    public IDictionary<string, RemovedWithDefinition> RemovedWith { get; set; } = new Dictionary<string, RemovedWithDefinition>();

    /// <summary>
    /// Gets or sets all the <see cref="RemovedWithJoinDefinition"/> for <see cref="EventType">event types</see>.
    /// </summary>
    public IDictionary<string, RemovedWithJoinDefinition> RemovedWithJoin { get; set; } = new Dictionary<string, RemovedWithJoinDefinition>();

    /// <summary>
    /// Gets or sets the optional <see cref="FromEventPropertyDefinition"/> definition.
    /// </summary>
    public FromEventPropertyDefinition? FromEventProperty { get; set; }

    /// <summary>
    /// Gets or sets the last time the projection definition was updated.
    /// </summary>
    public DateTimeOffset? LastUpdated { get; set; }

    /// <summary>
    /// Gets or sets the tags the projection belongs to.
    /// </summary>
    public IEnumerable<string> Tags { get; set; } = [];
}
