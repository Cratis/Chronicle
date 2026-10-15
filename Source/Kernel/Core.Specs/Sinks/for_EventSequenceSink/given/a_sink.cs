// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Sinks.for_EventSequenceSink.given;

public class a_sink : Specification
{
    protected const string Store = "store";
    protected const string Tenant = "tenant";
    protected static readonly EventType Target = new("TotalChanged", EventTypeGeneration.First);

    protected destinations _destinations;
    protected EventSequenceSink _sink;
    protected ReadModelDefinition _definition;
    protected EventSequenceSinkConfiguration _configuration;

    protected virtual EventSequenceId Destination => EventSequenceId.Outbox;

    protected virtual EventSequenceId? Configured => null;

    protected virtual EventStoreName StoreName => Store;

    void Establish()
    {
        _destinations = new();
        _configuration = new(Target, Configured);
        var schema = JsonSchema.FromJson("{\"type\":\"object\",\"properties\":{\"Total\":{\"type\":\"integer\"},\"Count\":{\"type\":\"integer\"}}}");
        _definition = new(
            "totals",
            "totals",
            "Totals",
            ReadModelOwner.None,
            ReadModelSource.Unknown,
            ReadModelObserverType.Projection,
            "totals-observer",
            new(SinkConfigurationId.None, WellKnownSinkTypes.EventSequence, _configuration),
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema },
            []);
        _sink = new(StoreName, Tenant, _definition, _configuration, _destinations.Grains, _destinations.Storage, _destinations.Converter);
    }

    protected static string TotalOf(ExpandoObject? state) => ((IDictionary<string, object?>)state!)["Total"]!.ToString()!;

    protected static IChangeset<AppendedEvent, ExpandoObject> Fold(ExpandoObject initial, long total, long count, ulong number, string subject = "someone", string correlation = "c1")
    {
        var context = EventContext.From("store", "tenant", new("Source", EventTypeGeneration.First), EventSourceType.Default, "key", EventStreamType.All, EventStreamId.Default, number, Guid.Parse(correlation == "c1" ? "00000000-0000-0000-0000-000000000001" : "00000000-0000-0000-0000-000000000002"), null, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), subject) with
        {
            CausedBy = Identity.System
        };
        var comparer = new ObjectComparer();
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(comparer, new AppendedEvent(context, new ExpandoObject()), initial);
        var next = new ExpandoObject();
        ((IDictionary<string, object?>)next)["Total"] = total;
        ((IDictionary<string, object?>)next)["Count"] = count;
        comparer.Compare(initial, next, out var differences);
        changeset.ReplaceState(next, differences);
        return changeset;
    }
}
