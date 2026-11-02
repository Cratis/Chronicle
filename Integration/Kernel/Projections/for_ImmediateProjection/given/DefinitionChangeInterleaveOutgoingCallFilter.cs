// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        if (context.Request.GetInterfaceType().FullName == "Cratis.Chronicle.Projections.IProjection" &&
            context.MethodName == "FoldForSingleReadModel" &&
            gate.TryEnter())
        {
            await gate.WaitForRelease();
        }

        await context.Invoke();
    }
}
