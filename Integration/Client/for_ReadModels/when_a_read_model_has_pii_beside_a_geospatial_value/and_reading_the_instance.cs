// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Geospatial;
using MongoDB.Bson;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_a_read_model_has_pii_beside_a_geospatial_value.and_reading_the_instance.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_a_read_model_has_pii_beside_a_geospatial_value;

/// <summary>
/// One <c>[PII]</c> concept turns compliance handling on for the whole read model, so every other value in it is
/// walked too — including a geospatial one, which is a schema leaf carrying only its format while the value on the
/// wire is a GeoJSON object. The walk used to read those GeoJSON members as properties the schema had lost, which
/// failed the partition on the first event carrying a location and left the read model permanently behind. The
/// geospatial value is public data and must come back untouched, while the personal one is still encrypted at rest.
/// </summary>
/// <param name="context">The test context.</param>
[Collection(ChronicleCollection.Name)]
public class and_reading_the_instance(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public EventSourceId VenueId { get; } = "pii-beside-geospatial-venue-1";
        public GatheringAnnounced Event { get; private set; } = default!;
        public Gathering? Instance { get; private set; }
        public BsonDocument? StoredDocument { get; private set; }

        public bool DocumentCanBeInspected => StoredReadModelDocument.CanBeInspected(ChronicleFixture);

        public override IEnumerable<Type> EventTypes => [typeof(GatheringAnnounced)];

        public override IEnumerable<Type> ModelBoundProjections => [typeof(Gathering)];

        void Establish() => Event = new GatheringAnnounced("Ada Lovelace", new Venue(new Point(10.7522, 59.9139), "Oslo"));

        async Task Because()
        {
            await EventStore.EventLog.Append(VenueId, Event);

            using var cts = new CancellationTokenSource(TimeSpanFactory.DefaultTimeout());
            while (Instance?.Venue is null)
            {
                Instance = await EventStore.ReadModels.GetInstanceById<Gathering>(VenueId.Value);
                if (Instance?.Venue is not null) break;
                await Task.Delay(200, cts.Token);
            }

            StoredDocument = await StoredReadModelDocument.Read(ChronicleFixture, "Gatherings");
        }
    }

    [Fact] void should_project_the_instance() => Context.Instance.ShouldNotBeNull();
    [Fact] void should_release_the_organizer() => Context.Instance!.Organizer.ShouldEqual(Context.Event.Organizer);
    [Fact] void should_keep_the_point_longitude() => Context.Instance!.Venue.Position.Longitude.ShouldEqual(Context.Event.Venue.Position.Longitude);
    [Fact] void should_keep_the_point_latitude() => Context.Instance!.Venue.Position.Latitude.ShouldEqual(Context.Event.Venue.Position.Latitude);
    [Fact] void should_keep_the_city_beside_the_point() => Context.Instance!.Venue.City.ShouldEqual(Context.Event.Venue.City);
    [Fact] void should_have_read_the_stored_document_when_the_backend_allows_it() => (!Context.DocumentCanBeInspected || Context.StoredDocument is not null).ShouldBeTrue();
    [Fact] void should_store_the_organizer_encrypted() => Context.StoredDocument?["Organizer"].AsString.ShouldNotEqual(Context.Event.Organizer.Value);
    [Fact] void should_keep_the_venue_shape_at_rest() => Context.StoredDocument?["Venue"].IsBsonDocument.ShouldBeTrue();
    [Fact] void should_store_the_city_in_the_clear() => Context.StoredDocument?["Venue"].AsBsonDocument["City"].AsString.ShouldEqual(Context.Event.Venue.City);
}

[PII]
public record Organizer(string Value) : ConceptAs<string>(Value)
{
    public static implicit operator Organizer(string value) => new(value);
}

public record Venue(Point Position, string City);

[EventType]
public record GatheringAnnounced(Organizer Organizer, Venue Venue);

[FromEvent<GatheringAnnounced>]
public record Gathering(string Id, Organizer Organizer, Venue Venue);

#pragma warning restore SA1402
