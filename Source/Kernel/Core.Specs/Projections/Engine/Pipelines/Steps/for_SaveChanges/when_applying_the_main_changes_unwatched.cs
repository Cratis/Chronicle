// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SaveChanges;

/// <summary>
/// Pins the shape that keeps the unwatched write path free of an extra state machine. Allocation cannot be measured reliably
/// here, as every substitute call allocates; the guarantee is that the method forwards the sink task and is not async.
/// </summary>
public class when_applying_the_main_changes_unwatched : Specification
{
    MethodInfo _method;

    void Because() => _method = typeof(SaveChanges).GetMethod("ApplyMainChanges", BindingFlags.Instance | BindingFlags.NonPublic)!;

    [Fact] void should_exist() => _method.ShouldNotBeNull();
    [Fact] void should_not_be_an_async_state_machine() => _method.GetCustomAttribute<AsyncStateMachineAttribute>().ShouldBeNull();
}
