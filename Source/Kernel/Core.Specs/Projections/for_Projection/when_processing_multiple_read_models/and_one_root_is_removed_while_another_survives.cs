// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_multiple_read_models;

public class and_one_root_is_removed_while_another_survives : given.a_projection_grain_with_a_child_projection
{
    const string RemovedKey = "the-removed-key";
    const string SurvivingKey = "the-surviving-key";
    readonly EventType _createdRemovedEventType = new("CreatedRemoved", EventTypeGeneration.First);
    readonly EventType _createdSurvivingEventType = new("CreatedSurviving", EventTypeGeneration.First);
    readonly EventType _removedEventType = new("Removed", EventTypeGeneration.First);
    IEnumerable<ExpandoObject> _result;
    IDictionary<string, object?> _survivingReadModel;

    void Establish()
    {
        _rootProjection.GetKeyResolverFor(Arg.Any<EventType>()).Returns(new KeyResolver((_, _, @event) =>
            Task.FromResult<KeyResolverResult>(new ResolvedKey(new Key(
                @event.Context.EventType == _createdSurvivingEventType ? SurvivingKey : RemovedKey,
                ArrayIndexers.NoIndexers)))));

        ProjectRootWith(context =>
        {
            if (context.Event.Context.EventType == _removedEventType)
            {
                context.Changeset.Remove();
                return;
            }

            var name = context.Event.Context.EventType == _createdSurvivingEventType ? "The surviving model" : "The removed model";
            context.Changeset.Add(new PropertiesChanged<ExpandoObject>(context.Changeset.CurrentState,
            [
                new PropertyDifference("Name", null, name)
            ]));
        });
    }

    async Task Because()
    {
        _result = (await ProcessForMultipleReadModels(
            AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_createdRemovedEventType, 1),
            AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_createdSurvivingEventType, 2),
            AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_removedEventType, 3))).ToList();
        _survivingReadModel = _result.Cast<IDictionary<string, object?>>().Single();
    }

    [Fact] void should_return_only_one_read_model() => _result.Count().ShouldEqual(1);
    [Fact] void should_not_return_the_removed_read_model() => _result.Cast<IDictionary<string, object?>>().Any(_ => _["id"]!.Equals(RemovedKey)).ShouldBeFalse();
    [Fact] void should_return_the_surviving_read_model_identifier() => _survivingReadModel["id"].ShouldEqual(SurvivingKey);
    [Fact] void should_preserve_the_surviving_read_model_name() => _survivingReadModel["Name"].ShouldEqual("The surviving model");
    [Fact] void should_preserve_the_surviving_read_model_sequence_number() => _survivingReadModel[WellKnownProperties.LastHandledEventSequenceNumber].ShouldEqual(2UL);
    [Fact] void should_return_only_the_complete_surviving_read_model() => _survivingReadModel.Count.ShouldEqual(3);
}
