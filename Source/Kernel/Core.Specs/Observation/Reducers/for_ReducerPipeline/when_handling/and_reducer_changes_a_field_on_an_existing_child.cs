// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_handling;

public class and_reducer_changes_a_field_on_an_existing_child : given.all_dependencies
{
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    PropertyDifference[] _differences;

    void Establish()
    {
        var initial = CreateTeam("Team One", "pending");
        ((IDictionary<string, object?>)initial)[WellKnownProperties.Subject] = EventSourceIdValue;

        var changed = CreateTeam("Team Two", "approved");

        _objectComparer = new ObjectComparer();

        _sink.FindOrDefault(Arg.Any<Concepts.Keys.Key>()).Returns(Task.FromResult<ExpandoObject?>(initial));
        _sink.ApplyChanges(
                Arg.Any<Concepts.Keys.Key>(),
                Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(),
                Arg.Any<EventSequenceNumber>())
            .Returns(callInfo =>
            {
                _changeset = callInfo.ArgAt<IChangeset<AppendedEvent, ExpandoObject>>(1);
                return Task.FromResult(Enumerable.Empty<FailedPartition>());
            });

        _pipeline = new ReducerPipeline(
            _readModelDefinition,
            _sink,
            _objectComparer,
            new ReadModelsCompliance(_complianceManager, _expandoObjectConverter),
            EventStore,
            EventStoreNamespace);

        Reducer = CreateReducer(changed);
    }

    ReducerDelegate Reducer { get; set; }

    async Task Because()
    {
        await _pipeline.Handle(CreateContext(EventSourceIdValue), Reducer);

        var propertiesChanged = _changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>().Single();
        _differences = propertiesChanged.Differences.ToArray();
    }

    [Fact] void should_apply_changes_to_the_sink() => _sink.Received(1).ApplyChanges(Arg.Any<Concepts.Keys.Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>());
    [Fact] void should_preserve_the_top_level_difference() => _differences.Any(_ => _.PropertyPath.Path == "name" && _.Original!.Equals("Team One") && _.Changed!.Equals("Team Two")).ShouldBeTrue();
    [Fact] void should_collapse_the_child_field_difference_to_the_collection() => _differences.Any(_ => _.PropertyPath.Path == "[members]").ShouldBeTrue();
    [Fact] void should_not_emit_the_indexerless_child_field_difference() => _differences.Any(_ => _.PropertyPath.Path == "[members].status").ShouldBeFalse();
    [Fact] void should_hold_the_original_collection() => GetMemberStatus(_differences.Single(_ => _.PropertyPath.Path == "[members]").Original!).ShouldEqual("pending");
    [Fact] void should_hold_the_changed_collection() => GetMemberStatus(_differences.Single(_ => _.PropertyPath.Path == "[members]").Changed!).ShouldEqual("approved");

    static ExpandoObject CreateTeam(string name, string memberStatus)
    {
        dynamic member = new ExpandoObject();
        member.memberId = "member-1";
        member.status = memberStatus;

        dynamic team = new ExpandoObject();
        team.id = EventSourceIdValue;
        team.name = name;
        team.members = new object[] { member };

        return team;
    }

    static string GetMemberStatus(object collection)
    {
        var member = ((IEnumerable<object>)collection).Cast<IDictionary<string, object?>>().Single();
        return (string)member["status"]!;
    }
}
