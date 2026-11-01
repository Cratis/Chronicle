// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using KernelProjection = KernelCore::Cratis.Chronicle.Projections.IProjection;

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ImmediateProjection.given;

/// <summary>
/// Places the test barrier before a fold is dispatched to the projection grain.
/// </summary>
/// <param name="gate">The test's fold gate.</param>
public class DefinitionChangeInterleaveOutgoingCallFilter(DefinitionChangeInterleaveGate gate) : IOutgoingGrainCallFilter
{
    /// <inheritdoc/>
    public async Task Invoke(IOutgoingGrainCallContext context)
    {
        if (context.InterfaceType == typeof(KernelProjection) &&
            context.MethodName == nameof(KernelProjection.FoldForSingleReadModel) &&
            gate.TryEnter())
        {
            await gate.WaitForRelease();
        }

        await context.Invoke();
    }
}
