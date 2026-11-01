// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.Serialization.for_AppendedEventSerializer;

public class when_writing_the_released_json_format : given.a_normalized_event
{
    byte[] _result;

    void Because() => _result = serializer.SerializeToArray(appended_event);

    [Fact] void should_be_byte_identical_to_the_v19_115_0_writer() => _result.ShouldEqual(released_format);
}
