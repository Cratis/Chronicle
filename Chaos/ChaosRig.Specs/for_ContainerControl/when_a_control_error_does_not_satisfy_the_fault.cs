// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Chaos.for_ContainerControl;

/// <summary>
/// Specifies that state tolerance never swallows an unrelated Docker control error.
/// </summary>
public class when_a_control_error_does_not_satisfy_the_fault : given.a_container_control
{
    InvalidOperationException _failure;
    Exception _caught;

    void Establish()
    {
        _failure = new("Docker control denied");
        State(_running);
        Fail(_failure);
        State(_running);
    }

    async Task Because() => _caught = await Catch.Exception(() => _control.Apply("kernel", "kill", CancellationToken.None));

    [Fact] void should_preserve_the_original_error() => _caught.ShouldEqual(_failure);
    [Fact] void should_inspect_the_actual_state_before_rejecting_tolerance() => _commands.Count.ShouldEqual(3);
    [Fact] void should_not_record_a_false_already_down_outcome() => _log.Exists(line => line.Contains("target already down")).ShouldBeFalse();
}
