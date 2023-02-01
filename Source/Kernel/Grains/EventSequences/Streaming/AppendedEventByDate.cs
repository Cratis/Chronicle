// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming;

/// <summary>
/// Represents an appended event with a date time.
/// </summary>
/// <param name="Event">The event to compare for.</param>
/// <param name="DateTime">The date time the event was added to cache.</param>
public record AppendedEventByDate(AppendedEvent Event, DateTimeOffset DateTime);
