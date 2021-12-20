// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.for_ProjectionResultStores
{
    public class when_getting_for_unknown_type : Specification
    {
        ProjectionResultStores stores;
        Exception result;

        void Establish() => stores = new ProjectionResultStores(new KnownInstancesOf<IProjectionResultStoreFactory>(Array.Empty<IProjectionResultStoreFactory>()));

        void Because() => result = Catch.Exception(() => stores.GetForTypeAndModel("bc5e82fd-9845-4464-9802-a7e21bd8a919", new Model("", null)));

        [Fact] void should_throw_unknown_projection_result_store() => result.ShouldBeOfExactType<UnknownProjectionResultStore>();
    }
}
