// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Benchmarks.NormalClient;

/// <summary>A fluent projection for one event.</summary>
/// <typeparam name="TEvent">The event.</typeparam>
public class FluentProjection<TEvent> : IProjectionFor<ProjectionModel<TEvent>>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<ProjectionModel<TEvent>> builder) => builder.From<TEvent>();
}
