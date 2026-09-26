// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Statistics;

/// <summary>
/// Represents how many events of one event type are held.
/// </summary>
/// <param name="EventType">The identifier of the event type.</param>
/// <param name="Count">How many events of the type are held.</param>
public record EventTypeCount(string EventType, long Count);
