// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Subjects;

namespace Cratis.Events.Projections.for_ProjectionPipeline
{
    public class when_events_are_provided_and_we_have_two_storage_providers : given.a_pipeline
    {
        static Guid key = Guid.NewGuid();
        ISubject<Event>   subject;
        Mock<IProjectionStorage>    first_storage;
        Mock<IProjectionStorage>    second_storage;
        ExpandoObject initial_state;
        Event @event;
        Changeset changeset;

        void Establish()
        {
            subject = new ReplaySubject<Event>();
            event_provider.Setup(_ => _.ProvideFor(projection.Object)).Returns(subject);
            first_storage = new();
            second_storage = new();
            initial_state = new();
            first_storage.Setup(_ => _.FindOrDefault(IsAny<object>())).Returns(Task.FromResult(initial_state));
            second_storage.Setup(_ => _.FindOrDefault(IsAny<object>())).Returns(Task.FromResult(initial_state));
            pipeline.StoreIn(first_storage.Object);
            pipeline.StoreIn(second_storage.Object);
            pipeline.Start();
            projection.Setup(_ => _.GetKeyResolverFor(IsAny<EventType>())).Returns((_) => key);
            @event = new Event(0, "8ccc9ac5-50bc-4791-97ca-8a03bc4a6ccf", DateTimeOffset.UtcNow, "14dea6ef-bc08-4fb3-86a3-21dedee157fd", new ExpandoObject());
            changeset = new Changeset(projection.Object, @event, initial_state);
            projection.Setup(_ => _.OnNext(@event, initial_state)).Returns(changeset);
        }

        void Because() => subject.OnNext(@event);

        [Fact] void should_find_model_using_key_for_first_storage() => first_storage.Verify(_ => _.FindOrDefault(key), Once());
        [Fact] void should_find_model_using_key_for_second_storage() => second_storage.Verify(_ => _.FindOrDefault(key), Once());
        [Fact] void should_call_projection_for_both_storages() => projection.Verify(_ => _.OnNext(@event, initial_state), Exactly(2));
        [Fact] void should_apply_changes_for_first_storage() => first_storage.Verify(_ => _.ApplyChanges(key, changeset), Once());
        [Fact] void should_apply_changes_for_second_storage() => second_storage.Verify(_ => _.ApplyChanges(key, changeset), Once());
    }
}
