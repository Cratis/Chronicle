// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventTypes;

/// <summary>
/// Represents the versioned schemas associated with an event type.
/// </summary>
public class EventTypeSchemas
{
    /// <summary>
    /// Gets or sets the versioned schemas associated with the event type.
    /// </summary>
    public IDictionary<uint, string> Schemas { get; set; } = new Dictionary<uint, string>();
}
