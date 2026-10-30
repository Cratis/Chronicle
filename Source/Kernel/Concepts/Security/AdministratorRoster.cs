// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Security;

/// <summary>
/// Identifies the single system administrator roster.
/// </summary>
public static class AdministratorRoster
{
    /// <summary>
    /// Gets the roster grain key.
    /// </summary>
    public const string Key = "system";

    /// <summary>
    /// Gets the event source on which administrator privileges are recorded.
    /// </summary>
    public static readonly EventSourceId EventSourceId = "administrator-roster";

    /// <summary>
    /// Gets the event source on which the one-time privilege migration is recorded.
    /// </summary>
    public static readonly EventSourceId MigrationEventSourceId = "administrator-privilege-migration";
}
