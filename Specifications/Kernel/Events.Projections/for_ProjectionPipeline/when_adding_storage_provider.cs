// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.for_ProjectionPipeline
{
    public class when_adding_storage_provider : given.a_pipeline
    {
        Mock<IProjectionResultStore> storage;

        void Establish() => storage = new();

        void Because() => pipeline.StoreIn("8a1e35ac-567c-4309-957d-61910eb0c581", storage.Object);

        [Fact] void should_have_storage_provider_added() => pipeline.ResultStores.First().ShouldEqual(storage.Object);
    }
}
