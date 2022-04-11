// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json.Nodes;
using Aksio.Cratis.Events.Store;

namespace Aksio.Cratis.Events.Projections.Pipelines.for_ProjectionPipeline.given;

public class a_pipeline_with_one_store : a_pipeline
{
    protected static ProjectionSinkConfigurationId configuration = "a4e5be99-85de-47c5-9399-c233933106d5";
    protected Mock<IProjectionSink> result_store;
    protected AppendedEvent @event;

    void Establish()
    {
        result_store = new();
        pipeline.StoreIn(configuration, result_store.Object);
        projection.Setup(_ => _.FilterEventTypes(IsAny<IObservable<AppendedEvent>>())).Returns((IObservable<AppendedEvent> observable) => observable);
        event_provider.Setup(_ => _.ProvideFor(pipeline, IsAny<ISubject<AppendedEvent>>())).Callback((IProjectionPipeline _, ISubject<AppendedEvent> sub) => subject = sub);
        @event = new(
            new(0,
            new("02405794-91e7-4e4f-8ad1-f043070ca297", 1)),
            new("2f005aaf-2f4e-4a47-92ea-63687ef74bd4", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "123b8935-a1a4-410d-aace-e340d48f0aa0", "41f18595-4748-4b01-88f7-4c0d0907aa90", "50308963-d8b5-4b6e-97c7-e2486e8237e1", "bfb7fd4a-1822-4937-a6d1-52464a173f84"),
            new JsonObject());
    }
}
