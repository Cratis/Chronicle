// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Chaos.for_ContainerControl;

/// <summary>
/// Specifies that already-down classification does not skip the recovery start.
/// </summary>
public class when_recovering_an_already_stopped_target : given.a_container_control
{
    string _result;

    void Establish()
    {
        State(_down);
        _responses.Enqueue(() => Task.FromResult("started"));
    }

    async Task Because() => _result = await _control.Apply("kernel", "start", CancellationToken.None);

    [Fact] void should_start_the_stopped_container() => string.Join(' ', _commands[^1]).ShouldEqual("start kernel");
    [Fact] void should_preserve_the_start_output() => _result.ShouldEqual("started");
    [Fact] void should_record_why_recovery_was_needed() => _log.Single().Contains("target already down").ShouldBeTrue();
    [Fact] void should_preserve_the_exit_diagnostics_before_start() => _log.Single().Contains("\"ExitCode\":134").ShouldBeTrue();
}
