// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps;

/// <summary>
/// Represents a <see cref="ICanPerformProjectionPipelineStep"/> that refuses an event whose projection contradicts a flat composite key.
/// </summary>
/// <remarks>
/// It runs after the event is handled and before anything is stored, so a refused event leaves no trace in the sink.
/// </remarks>
public class VerifyFlatCompositeKey : ICanPerformProjectionPipelineStep
{
    /// <inheritdoc/>
    public ValueTask<ProjectionEventContext> Perform(IProjection projection, ProjectionEventContext context)
    {
        if (!context.IsDeferred && !context.IsUnresolvable && !context.IsJoin && context.Key is FlatCompositeKey key)
        {
            FlatCompositeKeyState.Verify(context.Changeset, key);
        }

        return ValueTask.FromResult(context);
    }
}
