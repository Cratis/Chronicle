// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps;

/// <summary>
/// Defines protection of changes produced by a resolved projection future before persistence.
/// </summary>
public interface ICanProtectFutureChangesets
{
    /// <summary>
    /// Protect the future's changed values using the root read model schema and subject lineage.
    /// </summary>
    /// <param name="projection">The root projection.</param>
    /// <param name="futureContext">The resolved future context.</param>
    /// <returns>An awaitable task.</returns>
    ValueTask ProtectFutureChangeset(IProjection projection, ProjectionEventContext futureContext);
}
