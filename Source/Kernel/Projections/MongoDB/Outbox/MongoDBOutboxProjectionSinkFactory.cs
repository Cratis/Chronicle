// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.MongoDB;

/// <summary>
/// Represents a <see cref="IProjectionSinkFactory"/> for creating <see cref="MongoDBOutboxProjectionSink"/> instances.
/// </summary>
public class MongoDBOutboxProjectionSinkFactory : IProjectionSinkFactory
{
    /// <inheritdoc/>
    public ProjectionSinkTypeId TypeId => WellKnownProjectionSinkTypes.Outbox;

    /// <inheritdoc/>
    public IProjectionSink CreateFor(Model model) => new MongoDBOutboxProjectionSink();
}
