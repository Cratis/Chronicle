// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Events.Projections.Definitions;
using Orleans;

namespace Aksio.Cratis.Events.Projections.Grains;

/// <summary>
/// Defines an immediate projection.
/// </summary>
/// <remarks>
/// The compound identity is based on the actual event source id.
/// This ensures that we can run multiple of these in a cluster for a specific type without
/// having to wait for turn if its not the same identifier.
/// </remarks>
public interface IImmediateProjection : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Get the model instance.
    /// </summary>
    /// <param name="projectionDefinition">The <see cref="ProjectionDefinition"/> to use.</param>
    /// <returns>A projected model in the form of a <see cref="JsonObject"/>.</returns>
    Task<JsonObject> GetModelInstance(ProjectionDefinition projectionDefinition);
}
