// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Kernel.Configuration;

/// <summary>
/// Represents the configuration for the inbox of a microservice.
/// </summary>
public class Inbox
{
    /// <summary>
    /// Gets the other microservices outboxes to observe.
    /// </summary>
    public IList<Outbox> FromOutboxes { get; init; } = new List<Outbox>();

    /// <summary>
    /// Get all <see cref="MicroserviceId">microservice ids</see> configured.
    /// </summary>
    /// <returns>Collection of <see cref="MicroserviceId"/>.</returns>
    public IEnumerable<MicroserviceId> GetMicroserviceIds() => FromOutboxes.Select(_ => (MicroserviceId)_.Microservice).ToArray();
}
