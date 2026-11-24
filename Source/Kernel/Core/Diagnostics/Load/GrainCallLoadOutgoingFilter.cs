// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.Load;

/// <summary>
/// Counts the grain calls this silo has made that are still waiting for an answer.
/// </summary>
/// <param name="load">The <see cref="IKernelLoad"/> to report to.</param>
internal sealed class GrainCallLoadOutgoingFilter(IKernelLoad load) : IOutgoingGrainCallFilter
{
    /// <inheritdoc/>
    public async Task Invoke(IOutgoingGrainCallContext context)
    {
        var grainType = GrainTargets.GrainTypeOf(context.InterfaceMethod);
        var call = load.BeginPending(grainType, GrainTargets.EventStoreOf(grainType, KeyOf(context.TargetId)));
        try
        {
            await context.Invoke();
        }
        finally
        {
            load.EndPending(call);
        }
    }

    static string KeyOf(GrainId target) => target.Key.ToString() ?? string.Empty;
}
