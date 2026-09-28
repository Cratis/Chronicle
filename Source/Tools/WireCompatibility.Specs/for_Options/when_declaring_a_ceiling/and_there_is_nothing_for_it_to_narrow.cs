// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility.for_Options.when_declaring_a_ceiling;

/// <summary>
/// A ceiling only means anything against the set of releases a major produces. Accepting it next to an explicit
/// baseline would read as narrowing something and do nothing, and one from another major excludes nothing while
/// looking configured - either way the gate would end up switched off by accident.
/// </summary>
public class and_there_is_nothing_for_it_to_narrow : Specification
{
    [Fact]
    void should_refuse_a_ceiling_without_a_major() =>
        Catch.Exception(() => Options.Parse(["--baseline", "19.0.0", "--up-to", "19.4.9", "--current", "chronicle.desc"]))
            .ShouldBeOfExactType<InvalidArguments>();

    [Fact]
    void should_refuse_a_ceiling_from_another_major() =>
        Catch.Exception(() => Options.Parse(["--major", "19", "--up-to", "18.4.0", "--current", "chronicle.desc"]))
            .ShouldBeOfExactType<InvalidArguments>();
}
