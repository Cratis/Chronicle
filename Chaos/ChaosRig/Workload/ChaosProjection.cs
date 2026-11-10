// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Counts and sums contributions in the kernel projection pipeline.
/// </summary>
[Projection(Identifier)]
public sealed class ChaosProjection : IProjectionFor<ChaosProjectionModel>
{
    /// <summary>
    /// The stable observer identity shared with the drain oracle.
    /// </summary>
    public const string Identifier = "chronicle-chaos-projection";

    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<ChaosProjectionModel> builder) => builder
        .From<ChaosEvent>(from => from.Count(model => model.Count).Add(model => model.Sum).With(@event => @event.Payload));
}
