// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_notifying;

public class and_the_pipeline_owns_a_released_snapshot : given.a_subscriber_with_a_cached_pipeline
{
    AppendedEvent _event;
    protected ObserverSubscriberResult _result;
    protected JsonObject? _notified;
    protected virtual bool ProvidesReleasedSnapshot => true;

    void Establish()
    {
        _schema.Properties["name"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.String,
            ExtensionData = new Dictionary<string, object?> { [ComplianceJsonSchemaExtensions.ComplianceKey] = new[] { new ComplianceSchemaMetadata("PII", string.Empty) } }
        };
        _event = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(new EventType("event", 1), 42);
        var state = new ExpandoObject();
        ((IDictionary<string, object?>)state)["name"] = "protected sink value";
        var context = ProjectionEventContext.Empty(Substitute.For<IObjectComparer>(), _event);
        context.Changeset.InitialState = state;
        context.Changeset.Add(new PropertiesChanged<ExpandoObject>(state, [new PropertyDifference("name", null, "protected sink value")]));
        if (ProvidesReleasedSnapshot)
        {
            context.ReleasedReadModel = new ExpandoObject();
            ((IDictionary<string, object?>)context.ReleasedReadModel)["name"] = "released notification value";
        }
        _cachedPipeline.Handle(_event).Returns(context);
        _converter.ToJsonObject(Arg.Any<ExpandoObject>(), _schema).Returns(call => new JsonObject { ["name"] = (string)((IDictionary<string, object?>)call.ArgAt<ExpandoObject>(0))["name"]! });
        _notifier.When(notifier => notifier.Notify(Arg.Any<Concepts.EventStoreNamespaceName>(), Arg.Any<Concepts.ReadModels.ReadModelKey>(), Arg.Any<JsonObject>(), Arg.Any<Concepts.ReadModels.ReadModelChangeContext>()))
            .Do(call => _notified = call.ArgAt<JsonObject>(2));
    }

    async Task Because() => _result = await _subscriber.OnNext("the-partition", [_event], new(null));

    [Fact] void should_notify_successfully() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_publish_the_released_snapshot_instead_of_sink_state() => _notified!["name"]!.GetValue<string>().ShouldEqual("released notification value");
}
