// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.for_ProjectionPipeline
{
    public class when_adding_storage_provider : given.a_pipeline
    {
        Mock<IProjectionStorage> storage;

        void Establish() => storage = new();

        void Because() => pipeline.StoreIn(storage.Object);

        [Fact] void should_have_storage_provider_added() => pipeline.StorageProviders.First().ShouldEqual(storage.Object);
    }
}
