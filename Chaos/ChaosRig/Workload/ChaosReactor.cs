// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Records every delivery attempt without deduplication in the external side-ledger.
/// </summary>
[Reactor(Identifier)]
public sealed class ChaosReactor : IReactor
{
    /// <summary>
    /// The stable observer identity shared with the drain oracle.
    /// </summary>
    public const string Identifier = "chronicle-chaos-reactor";

    readonly SideLedger _ledger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChaosReactor"/> class.
    /// </summary>
    /// <param name="ledger">The independent delivery ledger.</param>
    internal ChaosReactor(SideLedger ledger) => _ledger = ledger;

    /// <summary>
    /// Records one externally visible delivery attempt.
    /// </summary>
    /// <param name="event">The delivered contribution.</param>
    /// <param name="context">The persisted source and position.</param>
    /// <returns>The durable insert task.</returns>
    public Task On(ChaosEvent @event, EventContext context) => _ledger.Record(@event.LedgerId, context.EventSourceId.Value, context.SequenceNumber.Value);
}
