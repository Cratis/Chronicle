// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionWatchGate;

public class when_opened : Specification
{
    ProjectionWatchGate _gate;

    void Establish() => _gate = new(new SettableTimeProvider());

    void Because() => _gate.Open(TimeSpan.FromSeconds(90));

    [Fact] void should_be_open() => _gate.IsOpen.ShouldBeTrue();
}
