// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.for_ProjectionResultStores
{
    public class when_asking_for_unknown_type : Specification
    {
        ProjectionResultStores  stores;
        bool result;

        void Establish() => stores = new ProjectionResultStores(new KnownInstancesOf<IProjectionResultStoreFactory>(Array.Empty<IProjectionResultStoreFactory>()));

        void Because() => result = stores.HasType("bc5e82fd-9845-4464-9802-a7e21bd8a919");

        [Fact] void should_not_have_type() => result.ShouldBeFalse();
    }
}
