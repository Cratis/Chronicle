// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_TypeFormats.when_getting_type_for_format;

public class with_uuid_alias : Specification
{
    TypeFormats _typeFormats;
    Type _result;

    void Establish() => _typeFormats = new();

    void Because() => _result = _typeFormats.GetTypeForFormat("uuid");

    [Fact] void should_return_guid_type() => _result.ShouldEqual(typeof(Guid));
}
