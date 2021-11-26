// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Changes;
using Newtonsoft.Json.Schema;

namespace Cratis.Events.Projections.for_ProjectionPipeline
{
    public class when_events_are_provided_and_we_have_two_result_stores : given.a_pipeline
    {
        static Guid key = Guid.Parse("c62c0ff8-61ac-4338-9278-0bcf674cec5f");
        static ProjectionResultStoreConfigurationId first_configuration = "d1bdab55-f61d-4954-80ce-b14d8ebc9bd4";
        static ProjectionResultStoreConfigurationId second_configuration = "df24629f-4db7-4095-a59f-b15e76f42369";
        ISubject<Event> first_store_subject;
        ISubject<Event> second_store_subject;
        Mock<IProjectionResultStore> first_store;
        Mock<IProjectionResultStore> second_store;
        ExpandoObject initial_state;
        Event @event;
        Model model;
        bool first_store_find_called;
        bool second_store_find_called;

        async Task Establish()
        {
            var callCount = 0;
            event_provider.Setup(_ => _.ProvideFor(projection.Object, IsAny<ISubject<Event>>())).Callback(
                (IProjection _, ISubject<Event> sub) =>
                {
                    switch (callCount)
                    {
                        case 0: first_store_subject = sub; break;
                        case 1: second_store_subject = sub; break;
                    }
                    callCount++;
                });
            first_store = new();
            second_store = new();
            initial_state = new();
            first_store.Setup(_ => _.FindOrDefault(IsAny<Model>(), IsAny<object>())).Returns(() =>
            {
                first_store_find_called = true;
                return Task.FromResult(initial_state);
            });
            first_store.SetupGet(_ => _.TypeId).Returns("1a793439-8a5a-4294-ba3c-9e4e7f75ade6");
            second_store.Setup(_ => _.FindOrDefault(IsAny<Model>(), IsAny<object>())).Returns(() =>
            {
                second_store_find_called = true;
                return Task.FromResult(initial_state);
            });

            second_store.SetupGet(_ => _.TypeId).Returns("3707090c-6c33-4461-afad-34560857ee0f");
            pipeline.StoreIn(first_configuration, first_store.Object);
            pipeline.StoreIn(second_configuration, second_store.Object);
            projection.Setup(_ => _.GetKeyResolverFor(IsAny<EventType>())).Returns((_) => key);
            @event = new Event(0, "8ccc9ac5-50bc-4791-97ca-8a03bc4a6ccf", DateTimeOffset.UtcNow, "14dea6ef-bc08-4fb3-86a3-21dedee157fd", new ExpandoObject());
            model = new Model(string.Empty, new JSchema());
            projection.SetupGet(_ => _.Model).Returns(model);
            projection_positions.Setup(_ => _.GetFor(projection.Object, first_configuration)).Returns(Task.FromResult(EventLogSequenceNumber.First));
            projection_positions.Setup(_ => _.GetFor(projection.Object, second_configuration)).Returns(Task.FromResult(EventLogSequenceNumber.First));

            var cursor = new Mock<IEventCursor>();
            cursor.Setup(_ => _.MoveNext()).Returns(Task.FromResult(false));
            cursor.SetupGet(_ => _.Current).Returns(Array.Empty<Event>());
            event_provider.Setup(_ => _.GetFromPosition(projection.Object, EventLogSequenceNumber.First)).Returns(Task.FromResult(cursor.Object));

            var tcs = new TaskCompletionSource();
            var subscription = pipeline.State.Where(_ => _ == ProjectionState.Active).Subscribe(_ =>
            {
                try
                {
                    tcs.SetResult();
                }
                catch { }
            });

            await pipeline.Start();
            await tcs.Task;
        }

        void Because()
        {
            first_store_subject.OnNext(@event);
            second_store_subject.OnNext(@event);
        }

        [Fact] void should_find_model_using_key_for_first_store() => first_store_find_called.ShouldBeTrue();
        [Fact] void should_find_model_using_key_for_second_store() => second_store_find_called.ShouldBeTrue();
        [Fact] void should_apply_changes_for_first_store() => first_store.Verify(_ => _.ApplyChanges(model, key, IsAny<Changeset<Event, ExpandoObject>>()), Once());
        [Fact] void should_apply_changes_for_second_store() => second_store.Verify(_ => _.ApplyChanges(model, key, IsAny<Changeset<Event, ExpandoObject>>()), Once());
        [Fact] void should_call_projection_for_both_stores() => projection.Verify(_ => _.OnNext(@event, IsAny<Changeset<Event, ExpandoObject>>()), Exactly(2));
    }
}
