// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Kernel.Persistence.EventSequences;

namespace Aksio.Cratis.Kernel.Projections;

/// <summary>
/// Represents the delegate for providing a value from an object.
/// </summary>
/// <param name="eventSequenceStorage"><see cref="IEventSequenceStorage"/> used.</param>
/// <param name="event">The <see cref="AppendedEvent"/> to resolve from.</param>
/// <returns>Resolved key.</returns>
public delegate Task<Key> KeyResolver(IEventSequenceStorage eventSequenceStorage, AppendedEvent @event);
