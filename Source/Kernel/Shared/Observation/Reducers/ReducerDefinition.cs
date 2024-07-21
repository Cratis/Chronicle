// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Models;
using Cratis.Chronicle.Sinks;

namespace Cratis.Chronicle.Reactions.Reducers;

/// <summary>
/// Represents the registration of a single client observer.
/// </summary>
/// <param name="ReducerId"><see cref="ReducerId"/> of the reducer.</param>
/// <param name="Name">The <see cref="ObserverName"/> of the reducer.</param>
/// <param name="EventSequenceId">The <see cref="EventSequenceId"/> the reducer is for.</param>
/// <param name="EventTypes">The type of events the observer is interested in.</param>
/// <param name="ReadModel">The <see cref="ModelDefinition"/> of the read model.</param>
/// <param name="SinkTypeId">Target sink.</param>
public record ReducerDefinition(
    ReducerId ReducerId,
    ObserverName Name,
    EventSequenceId EventSequenceId,
    IEnumerable<EventTypeWithKeyExpression> EventTypes,
    ModelDefinition ReadModel,
    SinkTypeId SinkTypeId);
