// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// The state of the delivery format validation of one registration request.
/// </summary>
/// <param name="eventStore">The <see cref="EventStoreName"/> the request is for.</param>
class WebhookAdmissionContext(EventStoreName eventStore)
{
    /// <summary>
    /// Gets the <see cref="EventStoreName"/> the request is for.
    /// </summary>
    public EventStoreName EventStore { get; } = eventStore;

    /// <summary>
    /// Gets the lifecycle position captured during validation for every new envelope webhook, keyed by identifier.
    /// </summary>
    public Dictionary<string, EventSequenceNumber> Positions { get; } = [];

    /// <summary>
    /// Gets the identifiers of the envelope webhooks already admitted in this request.
    /// </summary>
    public HashSet<string> Admitted { get; } = [];
}
