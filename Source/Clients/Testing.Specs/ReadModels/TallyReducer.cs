// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Reducer that increments <see cref="Tally.Count"/> for each <see cref="Tallied"/> event,
/// starting a fresh tally at 1 when there is no current state.
/// </summary>
public class TallyReducer : IReducerFor<Tally>
{
    /// <summary>
    /// Gets the unique identifier of the reducer.
    /// </summary>
    public ReducerId Id => "f3a1c7d2-9b4e-4c1a-8e2f-1d6b5a0c3e90";

    /// <summary>
    /// Increments the running count.
    /// </summary>
    /// <param name="event">The <see cref="Tallied"/> event.</param>
    /// <param name="current">The current <see cref="Tally"/> state, or <see langword="null"/> if none.</param>
    /// <param name="context">The <see cref="EventContext"/>.</param>
    /// <returns>The next <see cref="Tally"/> state.</returns>
    public Tally Increment(Tallied @event, Tally? current, EventContext context) =>
        current is null ? new Tally(Guid.Empty, 1) : current with { Count = current.Count + 1 };

    /// <summary>
    /// Records the event's sequence number into <see cref="Tally.Count"/>.
    /// </summary>
    /// <param name="event">The <see cref="SequenceProbed"/> event.</param>
    /// <param name="current">The current <see cref="Tally"/> state, or <see langword="null"/> if none.</param>
    /// <param name="context">The <see cref="EventContext"/>.</param>
    /// <returns>The next <see cref="Tally"/> state.</returns>
    public Tally Probe(SequenceProbed @event, Tally? current, EventContext context) =>
        new(Guid.Empty, (int)context.SequenceNumber.Value);

    /// <summary>
    /// Always throws, to simulate a reducer that fails while processing an event.
    /// </summary>
    /// <param name="event">The <see cref="TallyBroke"/> event.</param>
    /// <param name="current">The current <see cref="Tally"/> state, or <see langword="null"/> if none.</param>
    /// <param name="context">The <see cref="EventContext"/>.</param>
    /// <returns>Never returns; always throws.</returns>
    /// <exception cref="InvalidOperationException">Always thrown to simulate a failing reducer.</exception>
    public Tally Broke(TallyBroke @event, Tally? current, EventContext context) =>
        throw new InvalidOperationException("Reducer intentionally failed.");
}
