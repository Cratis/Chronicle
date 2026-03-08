// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Contracts.Events.Constraints;

namespace Cratis.Chronicle.Services.Events.Constraints;

/// <summary>
/// Represents the service for working with constraints.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> to use.</param>
internal sealed class Constraints(IGrainFactory grainFactory) : IConstraints
{
    /// <inheritdoc/>
    public async Task Register(RegisterConstraintsRequest request)
    {
        var key = new ConstraintsKey(request.EventStore);
        var grain = grainFactory.GetGrain<Chronicle.Events.Constraints.IConstraints>(key);

        var constraints = request.Constraints.Select(_ => _.ToChronicle()).ToArray();
        await grain.Register(constraints);
    }
}
