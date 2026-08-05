// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Registrations.for_RegistrationOutcome;

/// <summary>
/// The outcome is a record, so it says it compares by value, and it ships a <see cref="RegistrationOutcome.NotRun"/>
/// sentinel that invites being compared against. A record's generated equality compares its members with their own
/// equality, and a list's own equality is by reference - so two outcomes carrying the same artifacts came out unequal
/// and the type's headline promise did not hold.
/// </summary>
/// <remarks>
/// This is the same defect as the one fixed in the kernel's unique constraint definition on this branch, where
/// comparing covered events by reference made every re-registration look like a change. Worth pinning here rather
/// than trusting that nobody compares two outcomes.
/// </remarks>
public class when_comparing_two_outcomes_carrying_the_same_artifacts : Specification
{
    static readonly ArtifactRegistration _registered = new(typeof(string), null);

    RegistrationOutcome _outcome;
    RegistrationOutcome _equivalent;

    void Establish()
    {
        _outcome = new(true, ImmutableList.Create(_registered));
        _equivalent = new(true, ImmutableList.Create(_registered));
    }

    [Fact] void should_be_equal() => _outcome.Equals(_equivalent).ShouldBeTrue();
    [Fact] void should_hash_alike() => _outcome.GetHashCode().ShouldEqual(_equivalent.GetHashCode());

    [Fact]
    void should_not_be_equal_to_one_carrying_another_artifact() =>
        _outcome.Equals(new RegistrationOutcome(true, ImmutableList.Create(new ArtifactRegistration(typeof(int), null)))).ShouldBeFalse();

    [Fact]
    void should_not_be_equal_to_one_that_has_not_run() =>
        _outcome.Equals(new RegistrationOutcome(false, ImmutableList.Create(_registered))).ShouldBeFalse();

    [Fact]
    void should_equal_the_not_run_sentinel_when_it_carries_nothing_either() =>
        new RegistrationOutcome(false, ImmutableList<ArtifactRegistration>.Empty).Equals(RegistrationOutcome.NotRun).ShouldBeTrue();
}
