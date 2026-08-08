// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.Seeding;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage.Seeding;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Seeding.for_EventSeeding.given;

public class an_event_seeding_grain : Specification
{
    protected EventSeeding _grain;
    protected IPersistentState<EventSeeds> _state;
    protected IEventSequence _eventSequence;
    protected IGrainFactory _grainFactory;
    protected EventSeedingKey _key;
    protected ILogger<EventSeeding> _logger;

    void Establish()
    {
        _key = new EventSeedingKey("TestEventStore", "TestNamespace");
        _state = Substitute.For<IPersistentState<EventSeeds>>();
        _eventSequence = Substitute.For<IEventSequence>();
        _grainFactory = Substitute.For<IGrainFactory>();
        _logger = Substitute.For<ILogger<EventSeeding>>();

        // Only the error level is enabled, so a spec counting Log calls counts the one message that
        // reports a rejected batch and not the debug chatter the grain emits on every run.
        _logger.IsEnabled(LogLevel.Error).Returns(true);

        _state.State.Returns(new EventSeeds(
            new Dictionary<EventTypeId, IEnumerable<SeededEventEntry>>(),
            new Dictionary<EventSourceId, IEnumerable<SeededEventEntry>>()));

        AppendManySucceeds();

        _grain = new EventSeeding(_state, _grainFactory, _logger);

        // Simulate OnActivateAsync by setting internal fields via reflection
        var keyField = typeof(EventSeeding).GetField("_key", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        keyField.SetValue(_grain, _key);

        var eventSequenceField = typeof(EventSeeding).GetField("_eventSequence", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        eventSequenceField.SetValue(_grain, _eventSequence);
    }

    /// <summary>
    /// A result carrying a constraint violation - what the event sequence returns when one event in the
    /// batch is rejected. Nothing is appended when this comes back.
    /// </summary>
    /// <returns>A failed <see cref="AppendManyResult"/>.</returns>
    protected static AppendManyResult AViolatedResult() => new()
    {
        CorrelationId = CorrelationId.New(),
        ConstraintViolations =
        [
            new ConstraintViolation(
                "test-event-type",
                EventSequenceNumber.Unavailable,
                ConstraintType.Unique,
                "the-constraint",
                "the value is already taken",
                new ConstraintViolationDetails())
        ]
    };

    /// <summary>
    /// A result carrying an append error - the other half of the failure surface, reported for anything
    /// that is not a constraint violation.
    /// </summary>
    /// <returns>A failed <see cref="AppendManyResult"/>.</returns>
    protected static AppendManyResult AnErroredResult() => new()
    {
        CorrelationId = CorrelationId.New(),
        Errors = [new AppendError("something went wrong while appending")]
    };

    /// <summary>
    /// A result reporting that the whole batch was appended.
    /// </summary>
    /// <returns>A successful <see cref="AppendManyResult"/>.</returns>
    protected static AppendManyResult ASuccessfulResult() => AppendManyResult.Success(CorrelationId.New(), []);

    /// <summary>
    /// Gets every entry the grain has recorded as seeded, taken from the by-event-type half of the state.
    /// </summary>
    protected IEnumerable<SeededEventEntry> TrackedByEventType => _state.State.ByEventType.SelectMany(_ => _.Value);

    /// <summary>
    /// Gets every entry the grain has recorded as seeded, taken from the by-event-source half of the state.
    /// </summary>
    protected IEnumerable<SeededEventEntry> TrackedByEventSource => _state.State.ByEventSource.SelectMany(_ => _.Value);

    /// <summary>
    /// Gets the level the grain logged at, for the specs that assert a rejected batch is not silent.
    /// </summary>
    protected LogLevel LoggedLevel => (LogLevel)_logger.ReceivedCalls()
        .First(call => call.GetMethodInfo().Name == nameof(ILogger.Log))
        .GetArguments()[0]!;

    /// <summary>
    /// Makes every append report success.
    /// </summary>
    protected void AppendManySucceeds() => AppendManyReturns(_ => ASuccessfulResult());

    /// <summary>
    /// Makes every append return the result produced by the given factory, which receives the zero-based
    /// index of the call.
    /// </summary>
    /// <param name="resultForCall">Produces the result for a given call index.</param>
    protected void AppendManyReturns(Func<int, AppendManyResult> resultForCall)
    {
        var callCount = 0;
        _eventSequence.AppendMany(
            Arg.Any<IEnumerable<EventToAppend>>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
            Arg.Any<Concepts.Identities.Identity>(),
            Arg.Any<Concepts.EventSequences.Concurrency.ConcurrencyScopes>())
            .Returns(_ => Task.FromResult(resultForCall(callCount++)));
    }

    /// <summary>
    /// Builds a seeding entry whose content is derived from the given index, so a spec can name the exact
    /// entries it expects to survive a failed batch.
    /// </summary>
    /// <param name="index">Index to build for.</param>
    /// <returns>A <see cref="SeedingEntry"/>.</returns>
    protected static SeedingEntry AnEntry(int index) =>
        new($"event-source-{index}", "test-event-type", $"{{\"value\":\"test{index}\"}}", null);
}
