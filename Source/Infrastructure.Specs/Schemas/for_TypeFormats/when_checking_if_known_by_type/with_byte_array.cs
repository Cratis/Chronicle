// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_TypeFormats.when_checking_if_known_by_type;

public class with_byte_array : Specification
{
    TypeFormats _typeFormats;
    bool _result;

    void Establish() => _typeFormats = new();

    void Because() => _result = _typeFormats.IsKnown(typeof(byte[]));

    [Fact] void should_be_known() => _result.ShouldBeTrue();
    [Fact] void should_have_a_string_format() => _typeFormats.GetFormatForType(typeof(byte[])).ShouldEqual("byte-array");
}
