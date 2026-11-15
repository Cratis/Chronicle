// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Chaos.for_ContainerControl;

/// <summary>
/// Specifies that a missing target or inspection failure cannot masquerade as an already-down outcome.
/// </summary>
public class when_the_target_cannot_be_inspected : given.a_container_control
{
    InvalidOperationException _failure;
    Exception _caught;

    void Establish()
    {
        _failure = new("No such container");
        Fail(_failure);
    }

    async Task Because() => _caught = await Catch.Exception(() => _control.Apply("kernel", "kill", CancellationToken.None));

    [Fact] void should_preserve_the_inspection_failure() => _caught.ShouldEqual(_failure);
    [Fact] void should_not_attempt_control() => _commands.Count.ShouldEqual(1);
    [Fact] void should_not_log_an_unobserved_state() => _log.Count.ShouldEqual(0);
}
