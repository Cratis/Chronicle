// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Registrations.for_ProgramIdentifier;

public class when_checking_known_programs : Specification
{
    [Fact] void should_use_the_legacy_unknown_sentinel() => ProgramIdentifier.Unknown.Value.ShouldEqual("[N/A]");
    [Fact] void should_not_treat_the_sentinel_as_known() => new ProgramIdentifier("[N/A]").IsKnown.ShouldBeFalse();
    [Fact] void should_not_treat_an_omitted_identifier_as_known() => new ProgramIdentifier(string.Empty).IsKnown.ShouldBeFalse();
    [Fact] void should_recognize_a_program_identifier() => new ProgramIdentifier("orders").IsKnown.ShouldBeTrue();
}
