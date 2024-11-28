// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Models;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks;

public class when_getting_for_known_type : Specification
{
    static SinkTypeId _type = "df371e5d-b244-48d0-aaad-f298a127dd92";
    Sinks _stores;
    ISinkFactory _factory;
    ISink _store;
    ISink _result;
    Model _model;

    void Establish()
    {
        _model = new("Something", null!);
        _store = Substitute.For<ISink>();
        _factory = Substitute.For<ISinkFactory>();
        _factory.TypeId.Returns(_type);
        _factory.CreateFor(string.Empty, string.Empty, _model).Returns(_store);
        _stores = new(string.Empty, string.Empty, new KnownInstancesOf<ISinkFactory>([_factory]));
    }

    void Because() => _result = _stores.GetFor(_type, _model);

    [Fact] void should_create_and_return_store() => _result.ShouldEqual(_store);
}
