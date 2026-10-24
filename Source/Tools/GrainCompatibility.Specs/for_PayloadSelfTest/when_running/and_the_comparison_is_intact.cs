// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadSelfTest.when_running;

public class and_the_comparison_is_intact : Specification
{
    int _failures;
    string _output;

    void Because()
    {
        using var writer = new StringWriter();
        _failures = PayloadSelfTest.Run(writer);
        _output = writer.ToString();
    }

    [Fact] void should_detect_every_planted_case() => _failures.ShouldEqual(0);
    [Fact] void should_say_how_many_cases_it_ran() => _output.ShouldContain($"{PayloadSelfTest.Cases.Count} of {PayloadSelfTest.Cases.Count} planted payload cases");
}
