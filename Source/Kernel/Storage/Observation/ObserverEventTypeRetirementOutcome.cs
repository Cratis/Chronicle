// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Defines the durable final outcomes of an event-type retirement operation.
/// </summary>
public enum ObserverEventTypeRetirementOutcome
{
    /// <summary>
    /// The event type was not a member.
    /// </summary>
    NotMember = 0,

    /// <summary>
    /// A connected target still declared the event type.
    /// </summary>
    Refused = 1,

    /// <summary>
    /// The specified incarnation was removed.
    /// </summary>
    Retired = 2
}
