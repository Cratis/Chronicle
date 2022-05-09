// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Orleans;

namespace Aksio.Cratis.Events.Projections.Grains;

/// <summary>
/// Represents an implementation of <see cref="IImmediateProjection"/>.
/// </summary>
public class ImmediateProjection : Grain, IImmediateProjection
{
    /// <inheritdoc/>
    public override Task OnActivateAsync()
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<JsonObject> GetModelInstance() => throw new NotImplementedException();
}
