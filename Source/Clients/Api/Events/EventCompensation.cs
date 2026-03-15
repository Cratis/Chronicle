// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Api.Identities;

namespace Cratis.Chronicle.Api.Events;

/// <summary>
/// Represents a compensation applied to an event.
/// </summary>
/// <param name="Generation">The event type generation of this compensation.</param>
/// <param name="CorrelationId">The correlation id of the compensation.</param>
/// <param name="CausedBy">Who or what caused the compensation.</param>
/// <param name="Occurred">When the compensation occurred.</param>
/// <param name="Content">The JSON content of the compensating event.</param>
public record EventCompensation(
    uint Generation,
    string CorrelationId,
    Identity CausedBy,
    DateTimeOffset Occurred,
    string Content);
