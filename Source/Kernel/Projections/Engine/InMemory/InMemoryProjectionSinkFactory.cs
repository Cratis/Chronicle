// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.InMemory;

/// <summary>
/// Represents an implementation of <see cref="IProjectionSinkFactory"/> for <see cref="InMemoryProjectionSink"/>.
/// </summary>
public class InMemoryProjectionSinkFactory : IProjectionSinkFactory
{
    /// <inheritdoc/>
    public ProjectionSinkTypeId TypeId => InMemoryProjectionSink.ProjectionResultStoreTypeId;

    /// <inheritdoc/>
    public IProjectionSink CreateFor(Model model) => new InMemoryProjectionSink();
}
