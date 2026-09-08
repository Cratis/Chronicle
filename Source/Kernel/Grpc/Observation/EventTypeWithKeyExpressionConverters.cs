// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Extension methods for converting between <see cref="Contracts.Observation.EventTypeWithKeyExpression"/> and <see cref="EventTypeWithKeyExpression"/>.
/// </summary>
internal static class EventTypeWithKeyExpressionConverters
{
    /// <summary>
    /// Convert from <see cref="Contracts.Observation.EventTypeWithKeyExpression"/> to <see cref="EventTypeWithKeyExpression"/>.
    /// </summary>
    /// <param name="eventTypeWithKeyExpression"><see cref="Contracts.Observation.EventTypeWithKeyExpression"/> to convert from.</param>
    /// <returns>Converted <see cref="EventTypeWithKeyExpression"/>.</returns>
    public static EventTypeWithKeyExpression ToChronicle(this Contracts.Observation.EventTypeWithKeyExpression eventTypeWithKeyExpression) =>
        new(eventTypeWithKeyExpression.EventType.ToChronicle(), eventTypeWithKeyExpression.Key);
}
