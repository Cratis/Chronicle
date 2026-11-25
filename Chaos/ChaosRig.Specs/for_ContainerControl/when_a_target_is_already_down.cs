// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Chaos.for_ContainerControl;

/// <summary>
/// Specifies that already-down fault targets are logged without issuing a failing kill.
/// </summary>
public class when_a_target_is_already_down : given.a_container_control
{
    void Establish() => State(_down);

    async Task Because() => await _control.Apply("kernel", "kill", CancellationToken.None);

    [Fact] void should_only_inspect_the_target() => _commands.Count.ShouldEqual(1);
    [Fact] void should_record_the_already_down_outcome() => _log.Single().Contains("target already down").ShouldBeTrue();
    [Fact] void should_project_only_the_diagnostic_fields() => _commands.Single()[2].Contains(".Config").ShouldBeFalse();
}
