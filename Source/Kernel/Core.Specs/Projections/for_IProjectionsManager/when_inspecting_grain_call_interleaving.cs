// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Projections.for_IProjectionsManager;

/// <summary>
/// Reproduces #3848 - Ensure() does no work of its own (it only activates the grain), so it must not queue
/// FIFO behind whatever else is already pending on this non-reentrant grain, such as the client Register()
/// fan-out during a rolling redeploy. Without interleaving, the silo's own startup call to Ensure() can sit
/// behind that queue long enough to hit Orleans' response timeout and crash the host for work it never does.
/// </summary>
public class when_inspecting_grain_call_interleaving : Specification
{
    bool _ensureIsInterleaved;

    void Because() => _ensureIsInterleaved = IsInterleaved(typeof(IProjectionsManager).GetMethod(nameof(IProjectionsManager.Ensure))!);

    [Fact] void should_interleave_ensuring_the_projections_manager_exists() => _ensureIsInterleaved.ShouldBeTrue();

    static bool IsInterleaved(MethodInfo method) => Attribute.IsDefined(method, typeof(AlwaysInterleaveAttribute));
}
