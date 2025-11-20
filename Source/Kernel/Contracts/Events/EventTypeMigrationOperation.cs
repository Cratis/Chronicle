// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Events;

/// <summary>
/// Represents a migration operation for an event type.
/// </summary>
[ProtoContract]
public class EventTypeMigrationOperation
{
    /// <summary>
    /// Gets or sets the operation type.
    /// </summary>
    [ProtoMember(1)]
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the details of the operation as JSON.
    /// </summary>
    [ProtoMember(2)]
    public string Details { get; set; } = string.Empty;
}
