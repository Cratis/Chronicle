// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines an projection.
/// </summary>
/// <remarks>
/// The compound identity is based on the actual event source id.
/// This ensures that we can run multiple of these in a cluster for a specific type without
/// having to wait for turn if its not the same identifier.
/// </remarks>
public interface IImmediateProjection : IGrainWithStringKey
{
    /// <summary>
    /// Get the model instance.
    /// </summary>
    /// <returns>The <see cref="ProjectionResult"/>.</returns>
    Task<ProjectionResult> GetModelInstance();

    /// <summary>
    /// Dehydrate the projection instance.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Dehydrate();
}
