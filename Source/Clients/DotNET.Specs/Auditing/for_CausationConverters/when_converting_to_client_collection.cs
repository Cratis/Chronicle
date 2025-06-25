// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Auditing.for_CausationConverters;

public class when_converting_to_client_collection : Specification
{
    IEnumerable<Contracts.Auditing.Causation> _causations;
    IEnumerable<Causation> _result;
    DateTimeOffset _occurred;
    string _type;
    IDictionary<string, string> _properties;

    void Establish()
    {
        _occurred = DateTimeOffset.UtcNow;
        _type = "Root";
        _properties = new Dictionary<string, string> { { "key", "value" } };
        _causations = [new Contracts.Auditing.Causation { Occurred = _occurred, Type = _type, Properties = _properties }];
    }

    void Because() => _result = _causations.ToClient();

    [Fact] void should_return_a_collection_with_same_count() => _result.Count().ShouldEqual(1);
    [Fact] void should_map_occurred() => _result.First().Occurred.ShouldEqual(_occurred);
    [Fact] void should_map_type() => _result.First().Type.Value.ShouldEqual(_type);
    [Fact] void should_map_properties() => _result.First().Properties.ShouldEqual(_properties);
}
