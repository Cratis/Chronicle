// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility.for_Options.when_declaring_a_floor;

/// <summary>
/// A floor only means anything against the set of releases a major produces. Accepting it next to an explicit
/// baseline would read as narrowing something and do nothing, which is how a gate ends up switched off by accident.
/// </summary>
public class and_there_is_nothing_for_it_to_narrow : Specification
{
    [Fact]
    void should_refuse_a_floor_without_a_major() =>
        Catch.Exception(() => Options.Parse(["--baseline", "16.0.0", "--since", "16.36.0", "--current", "chronicle.desc"]))
            .ShouldBeOfExactType<InvalidArguments>();
}
