// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.ReadModels;

/// <summary>
/// Represents the context describing a change to a read model instance and the event that caused it.
/// </summary>
/// <param name="ChangeType">The <see cref="ReadModelChangeType"/> that occurred.</param>
/// <param name="EventSequenceNumber">The <see cref="EventSequenceNumber"/> of the event that caused the change.</param>
/// <param name="Occurred">When the event that caused the change occurred.</param>
/// <param name="CorrelationId">The <see cref="CorrelationId"/> of the event that caused the change.</param>
[GenerateSerializer]
[Alias(nameof(ReadModelChangeContext))]
public record ReadModelChangeContext(
    ReadModelChangeType ChangeType,
    EventSequenceNumber EventSequenceNumber,
    DateTimeOffset Occurred,
    CorrelationId CorrelationId);
