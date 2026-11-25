// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Chaos.for_ContainerControl;

/// <summary>
/// Specifies that an exit racing with a scheduled kill is recorded rather than aborting the run.
/// </summary>
public class when_a_target_exits_before_its_fault_applies : given.a_container_control
{
    string _result;

    void Establish()
    {
        State(_running);
        Fail(new InvalidOperationException("container is not running"));
        State(_down);
    }

    async Task Because() => _result = await _control.Apply("kernel", "kill", CancellationToken.None);

    [Fact] void should_complete_without_a_control_failure() => _result.ShouldEqual(string.Empty);
    [Fact] void should_reinspect_after_the_failed_kill() => _commands.Count.ShouldEqual(3);
    [Fact] void should_log_the_already_down_outcome() => _log[^1].Contains("target already down").ShouldBeTrue();
    [Fact] void should_log_the_exit_status() => _log[^1].Contains("\"ExitCode\":134").ShouldBeTrue();
    [Fact] void should_log_the_exit_reason() => _log[^1].Contains("\"Error\":\"process exited\"").ShouldBeTrue();
    [Fact] void should_log_the_exit_time() => _log[^1].Contains("2026-11-10T11:44:08Z").ShouldBeTrue();
    [Fact] void should_send_only_the_requested_kill() => string.Join(' ', _commands[1]).ShouldEqual("kill --signal KILL kernel");
}
