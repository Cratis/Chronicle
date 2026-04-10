// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Concepts.Observation.Reducers;

/// <summary>
/// Represents the registration of a single client observer.
/// </summary>
/// <param name="Identifier"><see cref="ReducerId"/> of the reducer.</param>
/// <param name="EventSequenceId">The <see cref="EventSequenceId"/> the reducer is for.</param>
/// <param name="EventTypes">The type of events the observer is interested in.</param>
/// <param name="ReadModel">The read model to use.</param>
/// <param name="IsActive">Whether or not the reducer is an actively observing reducer.</param>
/// <param name="Sink">Target sink.</param>
/// <param name="Tags">Collection of tags the reducer belongs to.</param>
/// <param name="EventSourceType">Optional <see cref="EventSourceType"/> filter. When <see cref="Events.EventSourceType.Unspecified"/>, all event source types are observed.</param>
/// <param name="EventStreamType">Optional <see cref="EventStreamType"/> filter. When <see cref="Events.EventStreamType.All"/>, all event stream types are observed.</param>
public record ReducerDefinition(
    ReducerId Identifier,
    EventSequenceId EventSequenceId,
    IEnumerable<EventTypeWithKeyExpression> EventTypes,
    ReadModelIdentifier ReadModel,
    bool IsActive,
    SinkDefinition Sink,
    IEnumerable<string> Tags,
    EventSourceType? EventSourceType = default,
    EventStreamType? EventStreamType = default);

