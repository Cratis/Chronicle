// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Storage.Sinks;
using Aksio.Cratis.Sinks;

namespace Aksio.Cratis.Kernel.Engines.Sinks.for_Sinks;

public class when_getting_for_known_type : Specification
{
    static SinkTypeId type = "df371e5d-b244-48d0-aaad-f298a127dd92";
    Storage.Sinks.Sinks stores;
    Mock<ISinkFactory> factory;
    Mock<ISink> store;
    ISink result;
    Model model;

    void Establish()
    {
        model = new("Something", null);
        store = new();
        factory = new();
        factory.SetupGet(_ => _.TypeId).Returns(type);
        factory.Setup(_ => _.CreateFor(model)).Returns(store.Object);
        stores = new(new KnownInstancesOf<ISinkFactory>(new[] { factory.Object }));
    }

    void Because() => result = stores.GetForTypeAndModel(type, model);

    [Fact] void should_create_and_return_store() => result.ShouldEqual(store.Object);
}
