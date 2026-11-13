// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager.given;

/// <summary>A <see cref="TimeProvider"/> whose time only moves when told to.</summary>
public class ManualTimeProvider : TimeProvider
{
    DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow() => _now;

    /// <summary>Moves the time forward.</summary>
    /// <param name="by">How far.</param>
    public void Advance(TimeSpan by) => _now += by;
}
