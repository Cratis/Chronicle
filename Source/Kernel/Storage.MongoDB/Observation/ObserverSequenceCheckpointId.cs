// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.MongoDB.Observation;

/// <summary>
/// Identifies a checkpoint by its observer and sequence, without concatenating identifiers.
/// </summary>
/// <param name="ObserverId">The observer.</param>
/// <param name="EventSequenceId">The sequence.</param>
public record ObserverSequenceCheckpointId(ObserverId ObserverId, EventSequenceId EventSequenceId);
