// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Captures.Engine;
using Cratis.Chronicle.Captures.Engine.DeclarationLanguage;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Captures;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Represents an implementation of <see cref="ICaptureEventsSubscriber"/> - translates the public events arriving in an
/// inbox into private events appended to the event log of the same event store and namespace.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for accessing captures and the event log.</param>
/// <param name="languageService"><see cref="ILanguageService"/> for compiling the capture declaration.</param>
/// <param name="translator"><see cref="ICaptureEventTranslator"/> for translating an incoming event.</param>
/// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for reading the incoming event content.</param>
/// <param name="logger">The logger.</param>
public class CaptureEventsSubscriber(
    IStorage storage,
    ILanguageService languageService,
    ICaptureEventTranslator translator,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<CaptureEventsSubscriber> logger) : Grain, ICaptureEventsSubscriber
{
    ObserverSubscriberKey _key = ObserverSubscriberKey.Unspecified;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _key = ObserverSubscriberKey.Parse(this.GetPrimaryKeyString());
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<ObserverSubscriberResult> OnNext(Key partition, IEnumerable<AppendedEvent> events, ObserverSubscriberContext context)
    {
        var lastHandled = EventSequenceNumber.Unavailable;
        try
        {
            if (!CaptureObservers.TryGetCaptureId(_key.ObserverId, out var captureId))
            {
                return ObserverSubscriberResult.Failed(lastHandled, $"The observer '{_key.ObserverId}' does not belong to a capture");
            }

            // Public events only ever arrive in an inbox. Observing anything else would turn private facts
            // into more private facts, which is not what an events capture is for.
            if (!_key.EventSequenceId.Value.StartsWith(EventSequenceId.InboxPrefix, StringComparison.Ordinal))
            {
                return ObserverSubscriberResult.Failed(lastHandled, $"The capture reads from '{_key.EventSequenceId}', which is not an inbox");
            }

            var eventStoreStorage = storage.GetEventStore(_key.EventStore);
            if (!await eventStoreStorage.Captures.Has(captureId))
            {
                return ObserverSubscriberResult.Failed(lastHandled, $"The capture {captureId} does not exist");
            }

            var capture = await eventStoreStorage.Captures.Get(captureId);
            var compilation = languageService.Compile(capture.Declaration);
            var definition = compilation.Match(
                definition => (CaptureDefinition?)(definition with { Id = captureId }),
                _ => null);
            if (definition is null)
            {
                return ObserverSubscriberResult.Failed(lastHandled, $"The declaration of capture '{capture.Name}' is invalid");
            }

            if (definition.Source.Type != SourceType.Events || definition.Source.Sequence != _key.EventSequenceId.Value)
            {
                return ObserverSubscriberResult.Failed(lastHandled, $"The capture '{capture.Name}' does not read from '{_key.EventSequenceId}'");
            }

            var observationId = CaptureObservers.ObservationIdFor(captureId, _key.Namespace);
            var observation = await eventStoreStorage.Captures.GetObservation(observationId);
            var state = observation.Items.ToDictionary(item => item.Key, item => JsonNode.Parse(item.Content)!.AsObject());

            foreach (var @event in events)
            {
                await Handle(capture, definition, eventStoreStorage.Captures, observationId, state, @event);
                lastHandled = @event.Context.SequenceNumber;
            }

            return ObserverSubscriberResult.Ok(lastHandled);
        }
        catch (Exception exception)
        {
            // The failure is reported to the observer, which fails the partition and keeps the event for
            // recovery. Nothing after the failing event is handled, and nothing is skipped.
            logger.CapturingEventsFailed(exception, _key.ObserverId, _key.EventSequenceId, _key.EventStore, _key.Namespace);
            return new ObserverSubscriberResult(
                ObserverSubscriberState.Failed,
                lastHandled,
                exception.GetAllMessages(),
                exception.StackTrace ?? string.Empty);
        }
    }

    /// <summary>
    /// A subject defaults to the event source id. Only a subject the incoming event was explicitly given - a
    /// natural person its content is about - follows into the private events, so erasing that person reaches
    /// what was derived from their events, and the default never leaks one event source's identity into another.
    /// </summary>
    /// <param name="source">The <see cref="EventContext"/> of the incoming event.</param>
    /// <returns>The <see cref="Subject"/> to carry over, or null when the event has none of its own.</returns>
    internal static Subject? ExplicitSubjectOf(EventContext source) =>
        source.Subject is { IsSet: true } subject && subject.Value != source.EventSourceId.Value ? subject : null;

    async Task Handle(
        Capture capture,
        CaptureDefinition definition,
        ICapturesStorage captures,
        CaptureId observationId,
        Dictionary<string, JsonObject> state,
        AppendedEvent @event)
    {
        var eventContext = @event.Context;
        if (!(definition.Source.Events ?? []).Contains(eventContext.EventType.Id.Value))
        {
            throw new UnsupportedCaptureCapability($"The capture '{capture.Name}' does not capture from '{eventContext.EventType.Id}'");
        }

        var content = JsonNode.Parse(JsonSerializer.Serialize(@event.Content, jsonSerializerOptions))?.AsObject() ?? [];
        var contextJson = CapturedEventContext.ToJson(eventContext);

        // The key is resolved before the state is looked up, so the translator is handed only the state of that key.
        var probe = translator.Translate(definition, null, content, contextJson);
        state.TryGetValue(probe.Key, out var previous);
        var translation = previous is null ? probe : translator.Translate(definition, previous, content, contextJson);

        if (translation.Events.Count > 0)
        {
            await AppendOnce(capture, eventContext, translation);
        }

        state[translation.Key] = translation.Current;
        await captures.SaveObservation(new CaptureObservation(
            observationId,
            state.Select(item => new CaptureObservedItem(item.Key, item.Value.ToJsonString())).ToList()));
    }

    async Task AppendOnce(Capture capture, EventContext source, CaptureEventTranslation translation)
    {
        var sourceTag = CaptureTags.ForSourceEvent(capture.Id, _key.EventSequenceId, source.SequenceNumber);
        var log = storage.GetEventStore(_key.EventStore).GetNamespace(_key.Namespace).GetEventSequence(EventSequenceId.Log);

        // Redelivery of an event we already translated - a crash between appending and remembering the
        // state, a recovered failed partition - finds its own tag in the log and appends nothing again.
        if ((await log.GetCount(tags: [sourceTag])).Value > 0)
        {
            logger.SkippingAlreadyCapturedEvent(capture.Name, source.SequenceNumber, _key.EventSequenceId);
            return;
        }

        var eventTypes = storage.GetEventStore(_key.EventStore).EventTypes;
        var toAppend = new List<EventToAppend>();
        foreach (var translated in translation.Events)
        {
            var eventTypeId = new EventTypeId(translated.Append.EventType);
            if (!await eventTypes.HasFor(eventTypeId))
            {
                throw new UnsupportedCaptureCapability($"The capture '{capture.Name}' appends '{translated.Append.EventType}', which is not a registered event type");
            }

            var schema = await eventTypes.GetFor(eventTypeId);
            toAppend.Add(new EventToAppend(
                EventSourceType.Default,
                translation.Key,
                EventStreamType.All,
                EventStreamId.Default,
                schema.Type,
                [.. CaptureTags.For(capture.Name), sourceTag],
                translated.Content,
                Subject: ExplicitSubjectOf(source)));
        }

        // The private events are a local fact recorded now. When the incoming event occurred at its origin is
        // kept in the causation, not in the occurred time of the facts we derive from it.
        var eventSequence = GrainFactory.GetGrain<IEventSequence>(
            new EventSequenceKey(EventSequenceId.Log, _key.EventStore, _key.Namespace));

        var result = await eventSequence.AppendMany(
            toAppend,
            source.CorrelationId,
            [CaptureCausation.ForSourceEvent(capture, source, _key.EventSequenceId)],
            Identity.System,
            new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

        if (!result.IsSuccess)
        {
            var reasons = result.Errors.Select(error => error.ToString())
                .Concat(result.ConstraintViolations.Select(violation => $"constraint violation: {violation.Message}"))
                .Concat(result.ConcurrencyViolations.Select(_ => "concurrency violation"));
            throw new CapturedEventsNotAppended(capture.Name, source.SequenceNumber, string.Join(", ", reasons));
        }
    }
}
