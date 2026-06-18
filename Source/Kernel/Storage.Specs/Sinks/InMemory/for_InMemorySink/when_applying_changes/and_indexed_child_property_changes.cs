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

namespace Cratis.Chronicle.Storage.Sinks.InMemory.for_InMemorySink.when_applying_changes;

public class and_indexed_child_property_changes : Specification
{
    Guid _candidateId;
    InMemorySink _sink;
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    Key _key;
    ExpandoObject? _result;
    IEnumerable<FailedPartition> _failedPartitions;

    void Establish()
    {
        _candidateId = Guid.NewGuid();
        _key = new Key("contract-1", ArrayIndexers.NoIndexers);
        _sink = new InMemorySink(CreateReadModelDefinition(), new TypeFormats());

        dynamic candidate = new ExpandoObject();
        candidate.candidateId = _candidateId;
        candidate.name = "Ada";
        candidate.isCustomerSigned = false;
        candidate.isPartnerSigned = false;

        dynamic state = new ExpandoObject();
        state.candidates = new List<object> { candidate };

        var arrayIndexers = new ArrayIndexers(
        [
            new ArrayIndexer(
                new PropertyPath("[candidates]"),
                new PropertyPath("candidateId"),
                _candidateId)
        ]);

        PropertyDifference[] differences =
        [
            new PropertyDifference(new PropertyPath("[candidates].isCustomerSigned"), false, true, arrayIndexers)
        ];
        var propertiesChanged = new PropertiesChanged<ExpandoObject>(state, differences);

        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.InitialState.Returns((ExpandoObject)state);
        Change[] changes = [propertiesChanged];
        _changeset.Changes.Returns(changes);
    }

    async Task Because()
    {
        _failedPartitions = await _sink.ApplyChanges(_key, _changeset, 42UL);
        _result = await _sink.FindOrDefault(_key);
    }

    [Fact] void should_not_fail() => _failedPartitions.ShouldBeEmpty();
    [Fact] void should_keep_one_candidate() => Candidates.Length.ShouldEqual(1);
    [Fact] void should_preserve_child_identifier() => Candidate["candidateId"].ShouldEqual(_candidateId);
    [Fact] void should_preserve_existing_child_field() => Candidate["name"].ShouldEqual("Ada");
    [Fact] void should_apply_changed_child_field() => Candidate["isCustomerSigned"].ShouldEqual(true);
    [Fact] void should_preserve_unrelated_child_field() => Candidate["isPartnerSigned"].ShouldEqual(false);

    ExpandoObject[] Candidates => ((IEnumerable<object>)((IDictionary<string, object?>)_result!)["candidates"]!).Cast<ExpandoObject>().ToArray();
    IDictionary<string, object?> Candidate => Candidates[0];

    static ReadModelDefinition CreateReadModelDefinition() =>
        new(
            "test-read-model",
            "TestReadModel",
            "TestReadModel",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { ReadModelGeneration.First, new JsonSchema() }
            },
            []);
}
