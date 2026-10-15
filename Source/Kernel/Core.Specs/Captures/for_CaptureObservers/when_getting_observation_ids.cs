// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CaptureObservers;

public class when_getting_observation_ids : Specification
{
    CaptureId _capture;
    CaptureId _first;
    CaptureId _sameAgain;
    CaptureId _otherNamespace;
    CaptureId _otherCapture;

    void Establish() => _capture = CaptureId.New();

    void Because()
    {
        _first = CaptureObservers.ObservationIdFor(_capture, "tenant-a");
        _sameAgain = CaptureObservers.ObservationIdFor(_capture, "tenant-a");
        _otherNamespace = CaptureObservers.ObservationIdFor(_capture, "tenant-b");
        _otherCapture = CaptureObservers.ObservationIdFor(CaptureId.New(), "tenant-a");
    }

    [Fact] void should_be_stable_for_a_namespace() => _sameAgain.ShouldEqual(_first);
    [Fact] void should_be_distinct_per_namespace() => _otherNamespace.ShouldNotEqual(_first);
    [Fact] void should_be_distinct_per_capture() => _otherCapture.ShouldNotEqual(_first);
}
