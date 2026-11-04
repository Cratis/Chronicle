// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_watching_a_read_model_with_pii.and_an_event_is_appended.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_watching_a_read_model_with_pii;

[Collection(ChronicleCollection.Name)]
public class and_an_event_is_appended(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public EventSourceId PatientId { get; } = "watched-pii-patient-1";
        public PatientNamed Event { get; } = new("Ada Lovelace");
        public ReadModelChangeset<PatientRecord> WatchResult;
        public EventSequenceNumber AppendedSequenceNumber;

        public override IEnumerable<Type> EventTypes => [typeof(PatientNamed)];
        public override IEnumerable<Type> ModelBoundProjections => [typeof(PatientRecord)];

        async Task Because()
        {
            var received = new TaskCompletionSource<ReadModelChangeset<PatientRecord>>(TaskCreationOptions.RunContinuationsAsynchronously);
            var watcher = EventStore.ReadModels.GetWatcherFor<PatientRecord>();
            using var subscription = watcher.Observable.Subscribe(result => received.TrySetResult(result));
            await watcher.Subscribed.WaitAsync(TimeSpanFactory.DefaultTimeout());
            var appended = await EventStore.EventLog.Append(PatientId, Event);
            AppendedSequenceNumber = appended.SequenceNumber;
            WatchResult = await received.Task.WaitAsync(TimeSpanFactory.DefaultTimeout());
        }
    }

    [Fact] void should_receive_the_released_value() => Context.WatchResult.ReadModel.Name.ShouldEqual(Context.Event.Name);
    [Fact] void should_carry_the_event_sequence_number() => Context.WatchResult.ChangeContext.SequenceNumber.ShouldEqual(Context.AppendedSequenceNumber);
    [Fact] void should_carry_the_sequence_number_on_a_declared_watermark() => Context.WatchResult.ReadModel.__lastHandledEventSequenceNumber.ShouldEqual(Context.AppendedSequenceNumber.Value);
}

[EventType]
public record PatientNamed([property: PII] string Name);

[FromEvent<PatientNamed>]
public record PatientRecord(string Id, [property: PII] string Name, ulong __lastHandledEventSequenceNumber);

#pragma warning restore SA1402
