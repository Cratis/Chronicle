// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility.for_Options.when_declaring_a_ceiling;

public class and_there_is_nothing_for_it_to_narrow : Specification
{
    [Fact]
    void should_refuse_a_ceiling_without_a_major() =>
        Catch.Exception(() => Options.Parse(["--baseline", "19.0.0", "--up-to", "19.4.9", "--current", "chronicle.desc"]))
            .ShouldBeOfExactType<InvalidArguments>();
}
