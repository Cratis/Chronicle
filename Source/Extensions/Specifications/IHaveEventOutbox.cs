// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Specifications;

/// <summary>
/// Defines a system for specifications that has an event log.
/// </summary>
public interface IHaveEventOutbox
{
    /// <summary>
    /// Gets the <see cref="IEventLog"/>.
    /// </summary>
    IEventOutbox EventOutbox { get; }

    /// <summary>
    /// Gets the collection of <see cref="AppendedEventForSpecifications"/>.
    /// </summary>
    IEnumerable<AppendedEventForSpecifications> AppendedEventsToOutbox { get; }
}
