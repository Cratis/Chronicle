// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionWatchGate;

public class when_closed_after_opening : Specification
{
    ProjectionWatchGate _gate;

    void Establish()
    {
        _gate = new(new SettableTimeProvider());
        _gate.Open(TimeSpan.FromSeconds(90));
    }

    void Because() => _gate.Close();

    [Fact] void should_be_closed() => _gate.IsOpen.ShouldBeFalse();
}
