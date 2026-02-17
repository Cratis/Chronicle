// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using NJsonSchema;

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks;

public class when_asking_for_known_type : Specification
{
    static SinkTypeId _type = "df371e5d-b244-48d0-aaad-f298a127dd92";
    Sinks _stores;
    ISinkFactory _factory;
    ISink _store;
    bool _result;
    ReadModelDefinition _model;

    void Establish()
    {
        _model = new(
            "SomethingId",
            "Something",
            "Something",
            ReadModelOwner.None,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>(),
            []);
        _store = Substitute.For<ISink>();
        _factory = Substitute.For<ISinkFactory>();
        _factory.TypeId.Returns(_type);
        _factory.CreateFor(string.Empty, string.Empty, _model).Returns(_store);
        _stores = new(string.Empty, string.Empty, new KnownInstancesOf<ISinkFactory>([_factory]));
    }

    void Because() => _result = _stores.HasType(_type);

    [Fact] void should_have_type() => _result.ShouldBeTrue();
}
