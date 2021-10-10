// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Subjects;

namespace Cratis.Events.Projections.for_ProjectionPipeline
{
    public class when_events_are_provided_and_we_have_two_storage_providers : given.a_pipeline
    {
        ISubject<Event>   subject;
        Mock<IProjectionStorage>    first_storage;
        Mock<IProjectionStorage>    second_storage;
        ExpandoObject initial_state;
        Event @event;

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
            @event = new Event(0, "8ccc9ac5-50bc-4791-97ca-8a03bc4a6ccf", DateTimeOffset.UtcNow, "14dea6ef-bc08-4fb3-86a3-21dedee157fd", new ExpandoObject());
        }

        void Because() => subject.OnNext(@event);

        [Fact] void should_call_projection_for_first_storage() => projection.Verify(_ => _.OnNext(@event, initial_state), Once());
        [Fact] void should_call_projection_for_second_storage() => projection.Verify(_ => _.OnNext(@event, initial_state), Once());
    }
}
