// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionWatchGate;

public class when_opening_the_shared_closed_gate : Specification
{
    void Because() => ProjectionWatchGate.Closed.Open(TimeSpan.FromSeconds(90));

    [Fact] void should_stay_closed() => ProjectionWatchGate.Closed.IsOpen.ShouldBeFalse();
}
