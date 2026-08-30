// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility.for_Options.when_parsing;

/// <summary>
/// A gate that runs without knowing what it is comparing is worse than no gate, because it reports an answer.
/// </summary>
public class and_the_arguments_do_not_describe_a_comparison : Specification
{
    [Fact]
    void should_refuse_without_a_current_contract() =>
        Catch.Exception(() => Options.Parse(["--major", "16"])).ShouldBeOfExactType<InvalidArguments>();

    [Fact]
    void should_refuse_without_a_baseline() =>
        Catch.Exception(() => Options.Parse(["--current", "chronicle.desc"])).ShouldBeOfExactType<InvalidArguments>();

    [Fact]
    void should_refuse_more_than_one_baseline() =>
        Catch.Exception(() => Options.Parse(["--major", "16", "--baseline", "16.0.0", "--current", "chronicle.desc"]))
            .ShouldBeOfExactType<InvalidArguments>();

    [Fact]
    void should_refuse_an_option_with_no_value() =>
        Catch.Exception(() => Options.Parse(["--current"])).ShouldBeOfExactType<InvalidArguments>();

    [Fact]
    void should_refuse_an_option_it_does_not_take() =>
        Catch.Exception(() => Options.Parse(["--current", "chronicle.desc", "--baseline", "16.0.0", "--nonsense"]))
            .ShouldBeOfExactType<InvalidArguments>();
}
