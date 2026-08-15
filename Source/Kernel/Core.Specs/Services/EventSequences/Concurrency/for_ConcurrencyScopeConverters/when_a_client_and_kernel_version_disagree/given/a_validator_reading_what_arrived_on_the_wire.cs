// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.EventSequences.Concurrency.for_ConcurrencyScopeConverters.when_a_client_and_kernel_version_disagree.given;

/// <summary>
/// Takes a contract scope exactly as it would arrive over the wire, converts it the way the kernel does, and runs
/// the real validator against it - so these specs assert what a mismatched deployment actually <em>does</em>,
/// rather than what the converted scope looks like.
/// </summary>
public class a_validator_reading_what_arrived_on_the_wire : Specification
{
    protected ConcurrencyValidator _validator;
    protected IEventSequenceStorage _eventSequenceStorage;
    protected EventSourceId _eventSourceId;
    protected Concepts.EventSequences.Concurrency.ConcurrencyScope _scope;
    protected Option<ConcurrencyViolation> _result;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        _validator = new ConcurrencyValidator(_eventSequenceStorage, NullLogger<ConcurrencyValidator>.Instance);
    }

    /// <summary>
    /// Seed the tail a matching event would produce, so a scope that really is checked has something to fail against.
    /// </summary>
    /// <param name="tail">The <see cref="EventSequenceNumber"/> the narrowed tail read answers.</param>
    protected void MatchingEventExistsAt(EventSequenceNumber tail) =>
        _eventSequenceStorage.GetTailSequenceNumber(
            Arg.Any<IEnumerable<EventType>>(),
            Arg.Any<EventSourceId>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventStreamType>()).Returns(tail);

    /// <summary>
    /// Convert a contract scope the way the kernel does and validate it.
    /// </summary>
    /// <param name="arrived">The <see cref="Contracts.EventSequences.Concurrency.ConcurrencyScope"/> that arrived.</param>
    /// <returns>Awaitable task.</returns>
    protected async Task Validate(Contracts.EventSequences.Concurrency.ConcurrencyScope arrived)
    {
        _scope = arrived.ToChronicle();
        _result = await _validator.Validate(_eventSourceId, _scope);
    }

    /// <summary>
    /// Build the contract scope a client puts on the wire for the first append into a narrowed scope.
    /// </summary>
    /// <param name="declaresTheExpectation">Whether the client is new enough to set the dedicated field.</param>
    /// <returns>The <see cref="Contracts.EventSequences.Concurrency.ConcurrencyScope"/> that arrives.</returns>
    protected static Contracts.EventSequences.Concurrency.ConcurrencyScope FirstAppendIntoANarrowedScope(bool declaresTheExpectation) => new()
    {
        SequenceNumber = EventSequenceNumber.Unavailable,
        ExpectsNoMatchingEvent = declaresTheExpectation,
        EventSourceId = true,
        EventSourceType = "Customer"
    };
}
