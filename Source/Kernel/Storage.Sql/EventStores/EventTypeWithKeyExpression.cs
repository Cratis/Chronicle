// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores;

/// <summary>
/// Represents an event type with an optional key expression.
/// </summary>
/// <param name="EventType">The event type.</param>
/// <param name="Generation">The generation of the event type.</param>
/// <param name="KeyExpression">The optional key expression.</param>
public record EventTypeWithKeyExpression(string EventType, uint Generation, string? KeyExpression);
