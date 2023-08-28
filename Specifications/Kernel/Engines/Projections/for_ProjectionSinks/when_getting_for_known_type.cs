// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Engines.Projections.for_ProjectionSinks;

public class when_getting_for_known_type : Specification
{
    static ProjectionId projection_id = "080a817f-ae60-4bbf-aeb6-75b7e89f97fc";
    static ProjectionSinkTypeId type = "df371e5d-b244-48d0-aaad-f298a127dd92";
    ProjectionSinks stores;
    Mock<IProjectionSinkFactory> factory;
    Mock<IProjectionSink> store;
    IProjectionSink result;
    Model model;

    void Establish()
    {
        model = new("Something", null);
        store = new();
        factory = new();
        factory.SetupGet(_ => _.TypeId).Returns(type);
        factory.Setup(_ => _.CreateFor(model)).Returns(store.Object);
        stores = new ProjectionSinks(new KnownInstancesOf<IProjectionSinkFactory>(new[] { factory.Object }));
    }

    void Because() => result = stores.GetForTypeAndModel(projection_id, type, model);

    [Fact] void should_create_and_return_store() => result.ShouldEqual(store.Object);
}
