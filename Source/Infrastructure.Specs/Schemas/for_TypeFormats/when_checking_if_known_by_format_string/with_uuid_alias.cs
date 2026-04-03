// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_TypeFormats.when_checking_if_known_by_format_string;

public class with_uuid_alias : Specification
{
    TypeFormats _typeFormats;
    bool _result;

    void Establish() => _typeFormats = new();

    void Because() => _result = _typeFormats.IsKnown("uuid");

    [Fact] void should_be_known() => _result.ShouldBeTrue();
}
