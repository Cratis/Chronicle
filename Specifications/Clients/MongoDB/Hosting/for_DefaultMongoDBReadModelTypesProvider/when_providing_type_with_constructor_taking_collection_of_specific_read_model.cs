// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.MongoDB;
using MongoDB.Driver;

namespace Aksio.Cratis.Hosting.for_DefaultMongoDBReadModelTypesProvider;

public class when_providing_type_with_constructor_taking_collection_of_specific_read_model : Specification
{
    record ReadModel();
    class TypeWithDependencies
    {
        public TypeWithDependencies(IMongoCollection<ReadModel> provider) { }
    }

    Mock<ICanProvideAssembliesForDiscovery> discoverer;
    DefaultMongoDBReadModelTypesProvider provider;
    IEnumerable<Type> types;

    void Establish()
    {
        discoverer = new();
        discoverer.SetupGet(_ => _.DefinedTypes).Returns(new[] { typeof(TypeWithDependencies) });
        provider = new DefaultMongoDBReadModelTypesProvider(discoverer.Object);
    }

    void Because() => types = provider.Provide();

    [Fact] void should_provide_read_model() => types.First().ShouldEqual(typeof(ReadModel));
}
