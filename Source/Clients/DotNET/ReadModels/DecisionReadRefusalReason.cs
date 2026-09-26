// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>Reasons a read cannot safely be used as a decision dependency.</summary>
public enum DecisionReadRefusalReason
{
    /// <summary>The model is reduced, not projected.</summary>
    Reducer = 0,

    /// <summary>There is not exactly one projection for the model.</summary>
    AmbiguousProjection = 1,

    /// <summary>The projection observes another sequence.</summary>
    NotEventLog = 2,

    /// <summary>The projection uses a join.</summary>
    Join = 3,

    /// <summary>The projection has children or nested projections.</summary>
    Hierarchy = 4,

    /// <summary>The projection subscribes to all events.</summary>
    OpenEndedEventTypes = 5,

    /// <summary>The projection includes derived event types.</summary>
    Derivatives = 6,

    /// <summary>The projection routes using an event property.</summary>
    FromEventProperty = 7,

    /// <summary>The projection is not routed by the raw event source ID.</summary>
    NotEventSourceKeyed = 8,

    /// <summary>The key schema converts source IDs to a different type.</summary>
    KeyConversion = 9,

    /// <summary>There are no event types to guard.</summary>
    NoEventTypes = 10,

    /// <summary>An event type ID cannot be encoded by the tail query.</summary>
    UnsupportedEventTypeId = 11,

    /// <summary>The key cannot be used as a scope label or raw source filter. GUID keys require canonical lowercase D form; this assumes all GUID event source IDs were written in that form.</summary>
    InvalidKey = 12,

    /// <summary>The fold did not reach an event visible to the diagnostic probe.</summary>
    FoldIncomplete = 13,

    /// <summary>The listed kernel definition disagrees with the client definition. This diagnostic does not certify agreement with the executing projection.</summary>
    DefinitionMismatch = 14
}
