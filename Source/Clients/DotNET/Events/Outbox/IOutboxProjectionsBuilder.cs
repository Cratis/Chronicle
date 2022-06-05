// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Projections;
using Aksio.Cratis.Events.Projections.Outbox;

namespace Aksio.Cratis.Events.Outbox;

/// <summary>
/// Defines the builder of projections for events to outbox.
/// </summary>
public interface IOutboxProjectionsBuilder
{
    /// <summary>
    /// Start building projections for a specific target event type.
    /// </summary>
    /// <param name="projectionBuilderCallback">Action for configuring the projection for the target event type.</param>
    /// <typeparam name="TTargetEvent">Type of target event type.</typeparam>
    /// <returns><see cref="IOutboxProjectionsBuilder"/> for continuation.</returns>
    IOutboxProjectionsBuilder For<TTargetEvent>(Action<IProjectionBuilderFor<TTargetEvent>> projectionBuilderCallback);

    /// <summary>
    /// Build the <see cref="OutboxProjectionsDefinition"/>.
    /// </summary>
    /// <returns>Ready built <see cref="OutboxProjectionsDefinition"/>.</returns>
    OutboxProjectionsDefinition Build();
}
