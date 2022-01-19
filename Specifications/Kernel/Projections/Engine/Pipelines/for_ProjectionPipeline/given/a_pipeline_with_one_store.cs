// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Subjects;

namespace Aksio.Cratis.Events.Projections.Pipelines.for_ProjectionPipeline.given
{
    public class a_pipeline_with_one_store : a_pipeline
    {
        protected static ProjectionResultStoreConfigurationId configuration = "a4e5be99-85de-47c5-9399-c233933106d5";
        protected Mock<IProjectionResultStore> result_store;
        protected Event @event;

        void Establish()
        {
            result_store = new();
            pipeline.StoreIn(configuration, result_store.Object);
            projection.Setup(_ => _.FilterEventTypes(IsAny<IObservable<Event>>())).Returns((IObservable<Event> observable) => observable);
            event_provider.Setup(_ => _.ProvideFor(pipeline, IsAny<ISubject<Event>>())).Callback((IProjectionPipeline _, ISubject<Event> sub) => subject = sub);
            @event = new(1, new EventType("22375ce3-3f80-4b42-97d4-27c416176e4c", 1), DateTimeOffset.UtcNow, "cc98878e-85eb-498a-ad0c-5a908c222f9c", new ExpandoObject());
        }
    }
}
