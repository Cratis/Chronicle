// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.Reducers.for_ReadModelStateFingerprint;

public class when_the_state_nests_too_deeply : given.states
{
    bool _result;

    void Because()
    {
        var state = Expando(("leaf", 1));
        for (var i = 0; i < 1000; i++)
        {
            state = Expando(("child", state));
        }

        _result = ReadModelStateFingerprint.TryCompute(state, out _);
    }

    [Fact] void should_not_produce_a_fingerprint() => _result.ShouldBeFalse();
}
