// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Auditing.for_CausationConverters;

public class when_converting_back_and_forth : Specification
{
    DateTimeOffset _occurred;
    CausationType _type;
    IDictionary<string, string> _properties;
    Causation _originalCausation;
    Causation _roundTrippedCausation;
    Contracts.Auditing.Causation _originalContract;

    void Establish()
    {
        _occurred = DateTimeOffset.UtcNow;
        _type = CausationType.Root;
        _properties = new Dictionary<string, string> { { "key", "value" } };
        _originalCausation = new(_occurred, _type, _properties);
        _originalContract = new Contracts.Auditing.Causation { Occurred = _occurred, Type = _type.Value, Properties = _properties };
    }

    void Because() => _roundTrippedCausation = _originalCausation.ToContract().ToClient();

    [Fact] void should_preserve_occurred() => _roundTrippedCausation.Occurred.ShouldEqual(_originalCausation.Occurred);
    [Fact] void should_preserve_type() => _roundTrippedCausation.Type.ShouldEqual(_originalCausation.Type);
    [Fact] void should_preserve_properties() => _roundTrippedCausation.Properties.ShouldEqual(_originalCausation.Properties);
}
