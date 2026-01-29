// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.EventSequences;

/// <summary>
/// Defines a calculator for generating content hashes for events.
/// </summary>
public interface IEventHashCalculator
{
    /// <summary>
    /// Calculate a hash for the event content.
    /// </summary>
    /// <param name="content">The event content to hash.</param>
    /// <returns>The calculated <see cref="EventHash"/>.</returns>
    EventHash Calculate(ExpandoObject content);
}
