// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Aksio.Cratis.Hosting.for_MongoDBReadModels

public class when_adding_read_models_with_type_taking_collection : Specification
{
    record ReadModel();
    class TypeWithDependencies
    {
        public TypeWithDependencies(IMongoCollection<ReadModel> provider) { }
    }

    Mock<IServiceCollection> service_collection;
    Mock<ITypes> types;

    ServiceDescriptor service_descriptor;

    void Establish()
    {
        service_collection = new();
        service_collection.Setup(_ => _.Add(IsAny<ServiceDescriptor>())).Callback((ServiceDescriptor sp) => service_descriptor = sp);
        types = new();
        types.SetupGet(_ => _.All).Returns(new[] { typeof(TypeWithDependencies) });
    }

    void Because() => service_collection.Object.AddMongoDBReadModels(types.Object);

    [Fact] void should_register_collection_provider() => service_descriptor.ServiceType.ShouldEqual(typeof(IMongoCollection<ReadModel>));
}
