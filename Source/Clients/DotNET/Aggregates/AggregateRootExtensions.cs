// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Extension methods for <see cref="IAggregateRoot"/>.
/// </summary>
public static class AggregateRootExtensions
{
    /// <summary>
    /// Get the <see cref="EventStreamType"/> for the <see cref="IAggregateRoot"/>.
    /// </summary>
    /// <param name="aggregateRoot"><see cref="IAggregateRoot"/> to get for.</param>
    /// <returns>The <see cref="EventStreamType"/> for the <see cref="IAggregateRoot"/>.</returns>
    public static EventStreamType GetEventStreamType(this IAggregateRoot aggregateRoot)
    {
        var attribute = aggregateRoot.GetType().GetCustomAttribute<EventStreamTypeAttribute>();
        return attribute?.EventStreamType ?? aggregateRoot.GetType().Name;
    }
}
