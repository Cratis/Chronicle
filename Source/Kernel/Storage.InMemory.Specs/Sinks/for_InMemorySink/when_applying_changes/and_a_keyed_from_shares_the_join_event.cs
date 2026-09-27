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

public class and_a_keyed_from_shares_the_join_event : Specification
{
    InMemorySink _sink;
    ExpandoObject? _result;
    ExpandoObject? _joinedResult;

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
        var existing = new ExpandoObject();
        var fields = (IDictionary<string, object?>)existing;
        fields["id"] = "joined-root";
        fields["joinId"] = "root";
        fields["joinedCount"] = 0L;
        _sink.Collection["joined-root"] = existing;
    }

    async Task Because()
    {
        var initial = new ExpandoObject();
        ((IDictionary<string, object?>)initial)["count"] = 0L;
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(initial);
        changeset.Changes.Returns(
        [
            new PropertiesChanged<ExpandoObject>(initial, [new PropertyDifference(new PropertyPath("count"), 0L, 1L)]),
            new Joined(
                initial,
                "root",
                new PropertyPath("joinId"),
                ArrayIndexers.NoIndexers,
                [new PropertiesChanged<ExpandoObject>(initial, [new PropertyDifference(new PropertyPath("joinedCount"), 0L, 1L)])])
            {
                HasKeyedFrom = true
            }
        ]);
        await _sink.ApplyChanges(new Key("root", ArrayIndexers.NoIndexers), changeset, EventSequenceNumber.Unavailable);
        _result = await _sink.FindOrDefault(new Key("root", ArrayIndexers.NoIndexers));
        _joinedResult = await _sink.FindOrDefault(new Key("joined-root", ArrayIndexers.NoIndexers));
    }

    [Fact] void should_persist_the_count_on_the_root_from_key() => ((IDictionary<string, object?>)_result!)["count"].ShouldEqual(1L);
    [Fact] void should_persist_the_joined_change_on_its_matching_root() => ((IDictionary<string, object?>)_joinedResult!)["joinedCount"].ShouldEqual(1L);
    [Fact] void should_not_apply_the_joined_change_to_the_from_root() => ((IDictionary<string, object?>)_result!).ContainsKey("joinedCount").ShouldBeFalse();
    [Fact] void should_leave_exactly_two_roots() => _sink.Collection.Count.ShouldEqual(2);

    record TestReadModel(string Id, string JoinId, long Count, long JoinedCount);
}
