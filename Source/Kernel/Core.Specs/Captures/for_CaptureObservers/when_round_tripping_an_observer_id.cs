// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CaptureObservers;

public class when_round_tripping_an_observer_id : Specification
{
    CaptureId _capture;
    CaptureId _resolved;
    bool _isCapture;
    bool _isSomethingElse;

    void Establish() => _capture = CaptureId.New();

    void Because()
    {
        _isCapture = CaptureObservers.TryGetCaptureId(CaptureObservers.For(_capture), out _resolved);
        _isSomethingElse = CaptureObservers.TryGetCaptureId("$system.patterns", out _);
    }

    [Fact] void should_recognize_a_capture_observer() => _isCapture.ShouldBeTrue();
    [Fact] void should_resolve_the_capture() => _resolved.ShouldEqual(_capture);
    [Fact] void should_not_recognize_other_observers() => _isSomethingElse.ShouldBeFalse();
}
