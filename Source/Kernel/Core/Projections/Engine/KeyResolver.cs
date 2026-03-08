// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Represents the delegate for providing a value from an object.
/// </summary>
/// <param name="eventSequenceStorage"><see cref="IEventSequenceStorage"/> used.</param>
/// <param name="sink"><see cref="ISink"/> for querying the read model.</param>
/// <param name="event">The <see cref="AppendedEvent"/> to resolve from.</param>
/// <returns>Resolved key or deferred future.</returns>
public delegate Task<KeyResolverResult> KeyResolver(IEventSequenceStorage eventSequenceStorage, ISink sink, AppendedEvent @event);
