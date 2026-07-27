// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using MongoDB.Bson;
using MongoDB.Driver;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_a_read_model_has_a_pii_value_object.and_reading_the_instance.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_a_read_model_has_a_pii_value_object;

/// <summary>
/// A whole value-object type marked <c>[PII]</c>, rather than a single concept inside it. Every value the type
/// holds is personal, so each of its leaves must be encrypted at rest and released on read — without collapsing
/// the value object into one opaque blob, which would change the document shape and break materialization.
/// </summary>
/// <param name="context">The test context.</param>
[Collection(ChronicleCollection.Name)]
public class and_reading_the_instance(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public EventSourceId PatientId { get; } = "pii-value-object-patient-1";
        public DiagnosisRecorded Event { get; private set; } = default!;
        public PatientRecord? Instance { get; private set; }
        public BsonDocument? StoredDocument { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(DiagnosisRecorded)];

        public override IEnumerable<Type> ModelBoundProjections => [typeof(PatientRecord)];

        void Establish() => Event = new DiagnosisRecorded("Ada Lovelace", new Diagnosis("Chronic migraine", "Dr. Babbage"));

        async Task Because()
        {
            await EventStore.EventLog.Append(PatientId, Event);

            using var cts = new CancellationTokenSource(TimeSpanFactory.DefaultTimeout());
            while (Instance?.Diagnosis is null)
            {
                Instance = await EventStore.ReadModels.GetInstanceById<PatientRecord>(PatientId.Value);
                if (Instance?.Diagnosis is not null) break;
                await Task.Delay(200, cts.Token);
            }

            StoredDocument = await ChronicleFixture.ReadModels.Database
                .GetCollection<BsonDocument>("PatientRecords")
                .Find(Builders<BsonDocument>.Filter.Empty)
                .FirstOrDefaultAsync();
        }
    }

    [Fact] void should_release_the_condition() => Context.Instance!.Diagnosis.Condition.ShouldEqual(Context.Event.Diagnosis.Condition);
    [Fact] void should_release_the_diagnosing_clinician() => Context.Instance!.Diagnosis.DiagnosedBy.ShouldEqual(Context.Event.Diagnosis.DiagnosedBy);
    [Fact] void should_store_the_condition_encrypted() => Context.StoredDocument!["Diagnosis"]["Condition"].AsString.ShouldNotEqual(Context.Event.Diagnosis.Condition);
    [Fact] void should_store_the_diagnosing_clinician_encrypted() => Context.StoredDocument!["Diagnosis"]["DiagnosedBy"].AsString.ShouldNotEqual(Context.Event.Diagnosis.DiagnosedBy);
    [Fact] void should_keep_the_value_object_shape_at_rest() => Context.StoredDocument!["Diagnosis"].IsBsonDocument.ShouldBeTrue();
    [Fact] void should_leave_the_non_pii_property_in_the_clear() => Context.StoredDocument!["Name"].AsString.ShouldEqual(Context.Event.Name);
}

[PII]
public record Diagnosis(string Condition, string DiagnosedBy);

[EventType]
public record DiagnosisRecorded(string Name, Diagnosis Diagnosis);

[FromEvent<DiagnosisRecorded>]
public record PatientRecord(string Id, string Name, Diagnosis Diagnosis);

#pragma warning restore SA1402
