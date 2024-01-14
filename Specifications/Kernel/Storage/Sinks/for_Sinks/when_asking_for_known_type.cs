// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Sinks;

namespace Aksio.Cratis.Kernel.Storage.Sinks.for_Sinks;

public class when_asking_for_known_type : Specification
{
    static SinkTypeId type = "df371e5d-b244-48d0-aaad-f298a127dd92";
    Sinks stores;
    Mock<ISinkFactory> factory;
    Mock<ISink> store;
    bool result;
    Model model;

    void Establish()
    {
        model = new("Something", null);
        store = new();
        factory = new();
        factory.SetupGet(_ => _.TypeId).Returns(type);
        factory.Setup(_ => _.CreateFor(string.Empty, string.Empty, model)).Returns(store.Object);
        stores = new(string.Empty, string.Empty, new KnownInstancesOf<ISinkFactory>(new[] { factory.Object }));
    }

    void Because() => result = stores.HasType(type);

    [Fact] void should_have_type() => result.ShouldBeTrue();
}
