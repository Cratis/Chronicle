// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.Reducers.for_ReducerStateFingerprints;

public class when_checking_a_remembered_partition : Specification
{
    ReducerStateFingerprints _fingerprints;

    void Establish()
    {
        _fingerprints = new(10);
        _fingerprints.Set("partition", 42);
        _fingerprints.Set("removed", 7);
        _fingerprints.Remove("removed");
    }

    [Fact] void should_be_unchanged_for_the_same_fingerprint() => _fingerprints.IsUnchanged("partition", 42).ShouldBeTrue();
    [Fact] void should_be_changed_for_another_fingerprint() => _fingerprints.IsUnchanged("partition", 43).ShouldBeFalse();
    [Fact] void should_be_changed_for_an_unknown_partition() => _fingerprints.IsUnchanged("unknown", 42).ShouldBeFalse();
    [Fact] void should_forget_a_removed_partition() => _fingerprints.Has("removed").ShouldBeFalse();
}
