// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.MongoDB;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Aksio.Cratis.Hosting.for_MongoDBReadModels;

public class when_adding_read_models_from_provider : Specification
{
    record ReadModel();

    Mock<IServiceCollection> service_collection;
    Mock<ICanProvideMongoDBReadModelTypes> types;

    ServiceDescriptor service_descriptor;

    void Establish()
    {
        service_collection = new();
        service_collection.Setup(_ => _.Add(IsAny<ServiceDescriptor>())).Callback((ServiceDescriptor sp) => service_descriptor = sp);
        types = new();
        types.Setup(_ => _.Provide()).Returns(new[] { typeof(ReadModel) });
    }

    void Because() => service_collection.Object.UseMongoDBReadModels(readModelTypeProvider: types.Object);

    [Fact] void should_register_collection_provider() => service_descriptor.ServiceType.ShouldEqual(typeof(IMongoCollection<ReadModel>));
}
