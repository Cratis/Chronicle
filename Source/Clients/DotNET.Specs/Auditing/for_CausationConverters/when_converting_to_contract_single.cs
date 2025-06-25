// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Auditing.for_CausationConverters;

public class when_converting_to_contract_single : Specification
{
    Causation _causation;
    Contracts.Auditing.Causation _result;
    DateTimeOffset _occurred;
    CausationType _type;
    IDictionary<string, string> _properties;

    void Establish()
    {
        _occurred = DateTimeOffset.UtcNow;
        _type = CausationType.Root;
        _properties = new Dictionary<string, string> { { "key", "value" } };
        _causation = new(_occurred, _type, _properties);
    }

    void Because() => _result = _causation.ToContract();

    [Fact] void should_map_occurred() => ((DateTimeOffset)_result.Occurred).ShouldEqual(_occurred);
    [Fact] void should_map_type() => _result.Type.ShouldEqual(_type.Value);
    [Fact] void should_map_properties() => _result.Properties.ShouldEqual(_properties);
}
