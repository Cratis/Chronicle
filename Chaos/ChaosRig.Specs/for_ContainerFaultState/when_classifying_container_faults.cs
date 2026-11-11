// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Chaos.for_ContainerFaultState;

/// <summary>
/// Specifies which container transitions are already satisfied and which still require Docker.
/// </summary>
public class when_classifying_container_faults : Specification
{
    ContainerFaultState _down;
    ContainerFaultState _running;
    ContainerFaultState _paused;
    ContainerFaultState _unknown;

    void Establish()
    {
        _down = new() { Status = "exited", ExitCode = 134, Error = "process exited", FinishedAt = "2026-11-10T11:44:08Z" };
        _running = new() { Status = "running", Running = true };
        _paused = _running with { Status = "paused", Paused = true };
        _unknown = new() { Status = "removing" };
    }

    [Fact] void should_skip_killing_an_exited_target() => _down.Classify("kill").Command.ShouldBeNull();
    [Fact] void should_skip_pausing_an_exited_target() => _down.Classify("pause").Command.ShouldBeNull();
    [Fact] void should_skip_unpausing_an_exited_target() => _down.Classify("unpause").Command.ShouldBeNull();
    [Fact] void should_record_the_already_down_outcome() => _down.Classify("kill").Outcome.ShouldEqual("target already down");
    [Fact] void should_still_start_an_exited_target() => _down.Classify("start").Command.ShouldEqual("start");
    [Fact] void should_report_the_exit_on_recovery() => _down.Classify("start").Outcome.ShouldEqual("target already down");
    [Fact] void should_kill_a_running_target() => _running.Classify("kill").Command.ShouldEqual("kill");
    [Fact] void should_pause_a_running_target() => _running.Classify("pause").Command.ShouldEqual("pause");
    [Fact] void should_skip_unpausing_a_non_paused_target() => _running.Classify("unpause").Command.ShouldBeNull();
    [Fact] void should_record_the_non_paused_outcome() => _running.Classify("unpause").Outcome.ShouldEqual("target not paused");
    [Fact] void should_not_restart_an_already_running_target() => _running.Classify("start").Command.ShouldBeNull();
    [Fact] void should_skip_pausing_a_paused_target() => _paused.Classify("pause").Command.ShouldBeNull();
    [Fact] void should_resume_a_paused_target() => _paused.Classify("unpause").Command.ShouldEqual("unpause");
    [Fact] void should_unpause_on_recovery_instead_of_starting_a_paused_target() => _paused.Classify("start").Command.ShouldEqual("unpause");
    [Fact] void should_allow_killing_a_paused_target() => _paused.Classify("kill").Command.ShouldEqual("kill");
    [Fact] void should_not_classify_unknown_states_as_already_down() => _unknown.Classify("kill").Command.ShouldEqual("kill");
    [Fact] void should_recognize_a_dead_target() => (_down with { Status = "dead" }).Classify("pause").Command.ShouldBeNull();
    [Fact] void should_recognize_a_created_target() => (_down with { Status = "created" }).Classify("kill").Command.ShouldBeNull();
    [Fact] void should_preserve_unsupported_commands_for_normal_failure_handling() => _down.Classify("unsupported").Command.ShouldEqual("unsupported");
}
