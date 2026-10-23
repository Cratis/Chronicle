// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.LegacyContracts.for_SelfTest.when_running;

public class and_the_gate_is_intact : Specification
{
    int _failures;

    void Because()
    {
        using var writer = new StringWriter();
        _failures = SelfTest.Run(writer);
    }

    [Fact] void should_detect_every_planted_case() => _failures.ShouldEqual(0);
}
