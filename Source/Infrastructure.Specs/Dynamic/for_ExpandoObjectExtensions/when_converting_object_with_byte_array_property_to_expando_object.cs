// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions;

public class when_converting_object_with_byte_array_property_to_expando_object : Specification
{
    byte[] _photoData;
    IDictionary<string, object?> _result;

    void Establish() => _photoData = [1, 2, 3, 255, 0];

    void Because() => _result = new WithByteArrayProperty(_photoData).AsExpandoObject(camelCaseProperties: true);

    [Fact] void should_keep_the_property_as_a_byte_array() => _result["photoData"].ShouldBeOfExactType<byte[]>();
    [Fact] void should_keep_the_same_bytes() => ((byte[])_result["photoData"]).ShouldContainOnly(_photoData);
}
