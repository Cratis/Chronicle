// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_ContainerStartFailureMessage;

public class when_describing_a_failed_start : Specification
{
    string _message;
    string _withoutExitCode;
    string _bounded;

    void Because()
    {
        var logs = "earlier\n" + string.Join('\n', Enumerable.Repeat("context", 18)) + "\nlast-but-one\nlast";
        _message = ContainerStartFailureMessage.Describe("example/kernel:local", 10, 1, logs, 20);
        _withoutExitCode = ContainerStartFailureMessage.Describe("example/kernel:local", 10, null, string.Empty, 20);
        _bounded = ContainerStartFailureMessage.Describe("example/kernel:local", 10, 1, new string('x', 10000) + "END\npassword=example-not-a-secret", 20);
    }

    [Fact] void should_identify_the_image() => _message.Contains("example/kernel:local", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_report_the_attempt_count() => _message.Contains("10 attempts", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_report_the_exit_code() => _message.Contains("exit code 1", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_include_the_last_lines() => _message.Contains("last-but-one" + Environment.NewLine + "last", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_omit_earlier_lines() => _message.Contains("earlier", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_state_when_the_exit_code_is_unavailable() => _withoutExitCode.Contains("exit code unavailable", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_bound_the_log_tail() => _bounded[(_bounded.IndexOf(Environment.NewLine, StringComparison.Ordinal) + Environment.NewLine.Length)..].Length.ShouldEqual(4096);
    [Fact] void should_keep_the_end_of_an_oversized_line() => _bounded.EndsWith("END", StringComparison.Ordinal).ShouldBeTrue();
}
