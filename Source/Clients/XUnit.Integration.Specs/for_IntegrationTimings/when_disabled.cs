// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.XUnit.Integration.for_IntegrationTimings;

public class when_disabled : Specification
{
    IntegrationTimings _timings;
    IntegrationTimings.Phase? _phase;
    int _calls;

    void Establish() => _timings = new(null, () => throw new InvalidOperationException("Clock must not be read"), 1000, "fixture");

    async Task Because()
    {
        _phase = _timings.Begin("boot");
        await _timings.Run("scenario", () => { _calls++; return Task.CompletedTask; });
        _timings.Complete();
    }

    [Fact] void should_not_allocate_a_phase() => _phase.ShouldBeNull();
    [Fact] void should_execute_the_original_action_once() => _calls.ShouldEqual(1);
}
