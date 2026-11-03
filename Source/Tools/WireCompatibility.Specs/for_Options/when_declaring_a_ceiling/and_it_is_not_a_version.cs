// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility.for_Options.when_declaring_a_ceiling;

public class and_it_is_not_a_version : Specification
{
    [Fact]
    void should_refuse_a_tag_prefix() =>
        Catch.Exception(() => Options.Parse(["--major", "19", "--up-to", "v19.4.9", "--current", "chronicle.desc"]))
            .ShouldBeOfExactType<InvalidArguments>();

    [Fact]
    void should_refuse_a_missing_patch() =>
        Catch.Exception(() => Options.Parse(["--major", "19", "--up-to", "19.4", "--current", "chronicle.desc"]))
            .ShouldBeOfExactType<InvalidArguments>();

    [Fact]
    void should_refuse_an_alias() =>
        Catch.Exception(() => Options.Parse(["--major", "19", "--up-to", "latest", "--current", "chronicle.desc"]))
            .ShouldBeOfExactType<InvalidArguments>();
}
