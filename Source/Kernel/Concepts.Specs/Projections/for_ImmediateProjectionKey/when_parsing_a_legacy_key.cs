// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.for_ImmediateProjectionKey;

public class when_parsing_a_legacy_key : Specification
{
    [Theory]
    [InlineData("projection#store#namespace#event-log#source")]
    [InlineData("projection#store#namespace#event-log#source#cb700f32-011f-41f9-a9b8-df71811394cd")]
    [InlineData("projection#store#namespace#event-log#$read-model-key##source#part")]
    [InlineData("projection#store#namespace#event-log#$read-model-key#cb700f32-011f-41f9-a9b8-df71811394cd#source#part")]
    public void should_preserve_the_legacy_serialization_byte_for_byte(string serialized)
    {
        var parsed = ImmediateProjectionKey.Parse(serialized);
        parsed.StreamScope.ShouldBeNull();
        parsed.ToString().ShouldEqual(serialized);
    }
}
