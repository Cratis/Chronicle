// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_applying_changes;

public class and_a_join_only_event_matches_an_existing_root : Specification
{
    InMemorySink _sink;
    ExpandoObject? _result;
    ExpandoObject? _secondResult;
    ExpandoObject? _phantom;

    void Establish()
    {
        var readModel = new ReadModelDefinition(
            "test",
            "test",
            "test",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                [ReadModelGeneration.First] = JsonSchema.FromType<TestReadModel>()
            },
            []);
        _sink = new InMemorySink(readModel, new TypeFormats());
        var row = new ExpandoObject();
        var fields = (IDictionary<string, object?>)row;
        fields["id"] = "root";
        fields["joinId"] = "join-source";
        fields["count"] = 0L;
        _sink.Collection["root"] = row;
        var secondRow = new ExpandoObject();
        var secondFields = (IDictionary<string, object?>)secondRow;
        secondFields["id"] = "second-root";
        secondFields["joinId"] = "join-source";
        secondFields["count"] = 0L;
        _sink.Collection["second-root"] = secondRow;
    }

    async Task Because()
    {
        var initial = new ExpandoObject();
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(initial);
        changeset.Changes.Returns(
        [
            new Joined(
                initial,
                "join-source",
                new PropertyPath("joinId"),
                ArrayIndexers.NoIndexers,
                [new PropertiesChanged<ExpandoObject>(initial, [new PropertyDifference(new PropertyPath("count"), 0L, 1L)])])
        ]);
        await _sink.ApplyChanges(new Key("join-source", ArrayIndexers.NoIndexers), changeset, EventSequenceNumber.Unavailable);
        _result = await _sink.FindOrDefault(new Key("root", ArrayIndexers.NoIndexers));
        _secondResult = await _sink.FindOrDefault(new Key("second-root", ArrayIndexers.NoIndexers));
        _phantom = await _sink.FindOrDefault(new Key("join-source", ArrayIndexers.NoIndexers));
    }

    [Fact] void should_persist_the_count_on_the_matching_root() => ((IDictionary<string, object?>)_result!)["count"].ShouldEqual(1L);
    [Fact] void should_persist_the_count_on_every_matching_root() => ((IDictionary<string, object?>)_secondResult!)["count"].ShouldEqual(1L);
    [Fact] void should_not_create_a_root_at_the_event_source_key() => _phantom.ShouldBeNull();

    record TestReadModel(string Id, string JoinId, long Count);
}
