// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Auditing.for_CausationConverters;

public class when_converting_back_and_forth_when_properties_are_null : Specification
{
    DateTimeOffset _occurred;
    CausationType _type;
    Causation _originalCausation;

    Causation _roundTrippedCausation;

    void Establish()
    {
        _occurred = DateTimeOffset.UtcNow;
        _type = CausationType.Root;
        _originalCausation = new(_occurred, _type, null!);
    }

    void Because() => _roundTrippedCausation = _originalCausation.ToContract().ToClient();

    [Fact] void should_initialize_properties_as_empty() => _roundTrippedCausation.Properties.ShouldBeEmpty();
}
