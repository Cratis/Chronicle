// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Specifications;

/// <summary>
/// Defines a system for specifications that has an event log.
/// </summary>
public interface IHaveEventLog
{
    /// <summary>
    /// Gets the <see cref="IEventLog"/>.
    /// </summary>
    IEventLog EventLog { get; }

    /// <summary>
    /// Gets the collection of <see cref="AppendedEventForSpecifications"/>.
    /// </summary>
    IEnumerable<AppendedEventForSpecifications> AppendedEvents { get; }
}
