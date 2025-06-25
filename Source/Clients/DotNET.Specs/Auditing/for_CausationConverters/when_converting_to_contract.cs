// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Auditing.for_CausationConverters;

public class when_converting_to_contract : Specification
{
    IEnumerable<Causation> _causations;
    IList<Contracts.Auditing.Causation> _result;
    DateTimeOffset _occurred;
    CausationType _type;
    IDictionary<string, string> _properties;

    void Establish()
    {
        _occurred = DateTimeOffset.UtcNow;
        _type = CausationType.Root;
        _properties = new Dictionary<string, string> { { "key", "value" } };
        _causations = [new Causation(_occurred, _type, _properties)];
    }

    void Because() => _result = _causations.ToContract();

    [Fact] void should_return_a_list_with_same_count() => _result.Count.ShouldEqual(1);
    [Fact] void should_map_occurred() => ((DateTimeOffset)_result[0].Occurred).ShouldEqual(_occurred);
    [Fact] void should_map_type() => _result[0].Type.ShouldEqual(_type.Value);
    [Fact] void should_map_properties() => _result[0].Properties.ShouldEqual(_properties);
}
