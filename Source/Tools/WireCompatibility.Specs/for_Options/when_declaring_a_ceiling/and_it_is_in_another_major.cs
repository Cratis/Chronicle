// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility.for_Options.when_declaring_a_ceiling;

public class and_it_is_in_another_major : Specification
{
    [Fact]
    void should_refuse_a_ceiling_from_another_major() =>
        Catch.Exception(() => Options.Parse(["--major", "19", "--up-to", "18.4.0", "--current", "chronicle.desc"]))
            .ShouldBeOfExactType<InvalidArguments>();
}
