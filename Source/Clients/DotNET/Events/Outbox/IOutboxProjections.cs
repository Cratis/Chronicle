// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Projections;

namespace Aksio.Cratis.Events.Outbox;

/// <summary>
/// Defines a system that is capable of defining projections from the event log to the event outbox.
/// </summary>
public interface IOutboxProjections
{
    /// <summary>
    /// Gets the unique identifier of thr projection.
    /// </summary>
    ProjectionId Identifier { get; }

    /// <summary>
    /// Define projections from events to public event types for outbox.
    /// </summary>
    /// <param name="builder">The <see cref="IOutboxProjectionsBuilder"/> to build on.</param>
    void Define(IOutboxProjectionsBuilder builder);
}
