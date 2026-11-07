// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.Reducers.for_ReducerStateFingerprints;

public class when_exceeding_the_bound : Specification
{
    ReducerStateFingerprints _fingerprints;

    void Establish() => _fingerprints = new(2);

    void Because()
    {
        _fingerprints.Set("a", 1);
        _fingerprints.Set("b", 2);
        _fingerprints.Set("c", 3);
    }

    [Fact] void should_not_hold_more_than_the_bound() => _fingerprints.Count.ShouldEqual(2);
    [Fact] void should_hold_the_newest() => _fingerprints.IsUnchanged("c", 3).ShouldBeTrue();
}
