// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Reducers;

/// <summary>
/// Represents the registration of a single client observer.
/// </summary>
public class ReducerDefinition
{
    /// <summary>
    /// Gets or sets the <see cref="ReducerId"/> of the reducer.
    /// </summary>
    public ReducerId Id { get; set; } = ReducerId.Unspecified;

    /// <summary>
    /// Gets or sets the <see cref="EventSequenceId"/> the reducer is for.
    /// </summary>
    public EventSequenceId EventSequenceId { get; set; } = EventSequenceId.Unspecified;

    /// <summary>
    /// Gets or sets the <see cref="EventTypeWithKeyExpression"/>s that the reducer subscribes to.
    /// </summary>
    public IDictionary<string, string> EventTypes { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the <see cref="ReadModelName"/> the reducer produces.
    /// </summary>
    public ReadModelName ReadModel { get; set; } = ReadModelName.NotSet;

    /// <summary>
    /// Gets or sets the <see cref="SinkDefinition"/> the reducer uses.
    /// </summary>
    public SinkDefinition Sink { get; set; } = SinkDefinition.None;
}
