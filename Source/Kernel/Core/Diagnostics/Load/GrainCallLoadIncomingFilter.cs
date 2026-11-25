// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.Load;

/// <summary>
/// Counts the calls running on the grains of this silo.
/// </summary>
/// <param name="load">The <see cref="IKernelLoad"/> to report to.</param>
internal sealed class GrainCallLoadIncomingFilter(IKernelLoad load) : IIncomingGrainCallFilter
{
    /// <inheritdoc/>
    public async Task Invoke(IIncomingGrainCallContext context)
    {
        var grainType = GrainTargets.GrainTypeOf(context.InterfaceMethod);
        var eventStore = GrainTargets.EventStoreOf(grainType, context.TargetId.Key.ToString() ?? string.Empty);
        load.BeginRunning(grainType, eventStore);
        try
        {
            await context.Invoke();
        }
        finally
        {
            load.EndRunning(grainType, eventStore);
        }
    }
}
