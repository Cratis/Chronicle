// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Contracts.Events.Constraints;

namespace Cratis.Chronicle.Services.Events.Constraints;

/// <summary>
/// Represents the service for working with constraints.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> to use.</param>
public class Constraints(IGrainFactory grainFactory) : IConstraints
{
    /// <inheritdoc/>
    public Task Register(RegisterConstraintsRequest request)
    {
        var key = new ConstraintsKey(request.EventStoreName);
        var grain = grainFactory.GetGrain<Grains.Events.Constraints.IConstraints>(key);

        // var constraints = request.Constraints.Select(_ => _.ToChronicle());
        // await grain.Register(constraints);
        throw new NotImplementedException();
    }
}
