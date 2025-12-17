// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.Observation.Reactors;

/// <summary>
/// Represents the registration of a single client observer.
/// </summary>
/// <param name="Identifier"><see cref="ReactorId"/> of the reducer.</param>
/// <param name="Owner">The owner of the reactor.</param>
/// <param name="EventSequenceId">The <see cref="EventSequenceId"/> the reducer is for.</param>
/// <param name="EventTypes">The type of events the observer is interested in.</param>
/// <param name="IsReplayable">Whether the reactor supports replay scenarios.</param>
/// <param name="Categories">Collection of categories the reactor belongs to.</param>
public record ReactorDefinition(
    ReactorId Identifier,
    ReactorOwner Owner,
    EventSequenceId EventSequenceId,
    IEnumerable<EventTypeWithKeyExpression> EventTypes,
    bool IsReplayable = true,
    IEnumerable<string>? Categories = default);
