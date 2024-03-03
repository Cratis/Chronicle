// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Cratis.Kernel.Keys;
using Cratis.Kernel.Storage.EventSequences;

namespace Cratis.Kernel.Projections;

/// <summary>
/// Represents the delegate for providing a value from an object.
/// </summary>
/// <param name="eventSequenceStorage"><see cref="IEventSequenceStorage"/> used.</param>
/// <param name="event">The <see cref="AppendedEvent"/> to resolve from.</param>
/// <returns>Resolved key.</returns>
public delegate Task<Key> KeyResolver(IEventSequenceStorage eventSequenceStorage, AppendedEvent @event);
