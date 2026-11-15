// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_a_nested_object_property_is_set_once : given.a_child_collection_projection
{
    static readonly EventType _alertRaised = new("AlertRaised", EventTypeGeneration.First);
    IProjection _projection;
    object? _afterFirst;
    object? _afterSecond;
    PropertyDifference[] _secondDifferences;

    void Establish()
    {
        _readModel.Schemas[ReadModelGeneration.First] = JsonSchema.FromJson("""
            {"type":"object","properties":{
              "id":{"type":"string"},
              "details":{"type":"object","properties":{"raisedBy":{"type":"string"}}}
            }}
            """);
        _definition.Children.Clear();
        _definition.From[_alertRaised] = new(
            new Dictionary<PropertyPath, string> { ["details.raisedBy"] = $"{WellKnownExpressions.SetOnce}(by)" },
            "$eventSourceId",
            null);
    }

    async Task Because()
    {
        _projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, []);
        var first = Project("alice", new ExpandoObject());
        _afterFirst = RaisedBy(first.CurrentState);
        var second = Project("bob", first.CurrentState);
        _afterSecond = RaisedBy(second.CurrentState);
        _secondDifferences = [.. second.Changes.OfType<PropertiesChanged<ExpandoObject>>().SelectMany(_ => _.Differences)];
    }

    Changeset<AppendedEvent, ExpandoObject> Project(string by, ExpandoObject state)
    {
        var incoming = new AppendedEvent(
            EventContext.EmptyWithEventSourceId("alert") with { EventType = _alertRaised },
            new { by }.AsExpandoObject());
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), incoming, state);
        _projection.OnNext(new ProjectionEventContext(new Key("alert", ArrayIndexers.NoIndexers), incoming, changeset, _projection.GetOperationTypeFor(_alertRaised), false));
        return changeset;
    }

    static object? RaisedBy(ExpandoObject state) => ((IDictionary<string, object?>)((IDictionary<string, object?>)state)["details"]!)["raisedBy"];

    [Fact] void should_set_the_nested_value_the_first_time() => _afterFirst.ShouldEqual("alice");
    [Fact] void should_keep_the_first_nested_value() => _afterSecond.ShouldEqual("alice");
    [Fact] void should_not_report_a_change_the_second_time() => _secondDifferences.ShouldBeEmpty();
}
