// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Pipelines.for_ProjectionPipeline
{
    public class when_adding_store : given.a_pipeline
    {
        static ProjectionResultStoreConfigurationId id = "8a1e35ac-567c-4309-957d-61910eb0c581";

        Mock<IProjectionResultStore> storage;

        void Establish() => storage = new();

        void Because() => pipeline.StoreIn(id, storage.Object);

        [Fact] void should_hold_the_added_store() => pipeline.ResultStores.First().Value.ShouldEqual(storage.Object);
    }
}
