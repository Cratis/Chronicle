// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Auditing.for_CausationConverters;

public class when_converting_to_client_single_with_null_properties : Specification
{
    Contracts.Auditing.Causation _causation;
    Causation _result;
    DateTimeOffset _occurred;
    string _type;

    void Establish()
    {
        _occurred = DateTimeOffset.UtcNow;
        _type = "Root";
        _causation = new Contracts.Auditing.Causation { Occurred = _occurred, Type = _type, Properties = null! };
    }

    void Because() => _result = _causation.ToClient();

    [Fact] void should_map_occurred() => _result.Occurred.ShouldEqual(_occurred);
    [Fact] void should_map_type() => _result.Type.Value.ShouldEqual(_type);
    [Fact] void should_initialize_properties_as_empty() => _result.Properties.ShouldBeEmpty();
}
