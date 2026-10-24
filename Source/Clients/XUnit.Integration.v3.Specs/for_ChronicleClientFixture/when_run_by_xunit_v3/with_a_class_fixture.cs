// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleClientFixture.when_run_by_xunit_v3;

/// <summary>
/// Exercises Given's class fixture through xUnit v3 discovery and lifetime handling.
/// </summary>
/// <param name="fixture">The runner-created fixture.</param>
public class with_a_class_fixture(runner_owned_fixture fixture) : Given<runner_owned_fixture>(fixture)
{
    /// <summary>
    /// Verifies initialization has completed before the spec executes.
    /// </summary>
    [Fact]
    public void should_initialize_the_task_hook_once() => Context.InitializeCount.ShouldEqual(1);

    /// <summary>
    /// Verifies the scenario scope stays alive until runner teardown.
    /// </summary>
    [Fact]
    public void should_keep_the_scope_alive_during_the_spec() => Context.ScopeIsAlive.ShouldBeTrue();
}
