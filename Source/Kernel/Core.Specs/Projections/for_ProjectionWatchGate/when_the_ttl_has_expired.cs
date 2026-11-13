// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionWatchGate;

public class when_the_ttl_has_expired : Specification
{
    SettableTimeProvider _time;
    ProjectionWatchGate _gate;
    bool _openJustBeforeExpiry;

    void Establish()
    {
        _time = new();
        _gate = new(_time);
        _gate.Open(TimeSpan.FromSeconds(90));
    }

    void Because()
    {
        _time.Seconds += 89;
        _openJustBeforeExpiry = _gate.IsOpen;
        _time.Seconds++;
    }

    [Fact] void should_be_open_until_the_ttl_has_elapsed() => _openJustBeforeExpiry.ShouldBeTrue();
    [Fact] void should_be_closed() => _gate.IsOpen.ShouldBeFalse();
}
