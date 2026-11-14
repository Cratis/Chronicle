// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents an event store and namespace that holds encryption keys stored under earlier names.
/// </summary>
/// <remarks>
/// Earlier secret names are lowercased and reduced to letters, digits and hyphens, so the event store and the namespace
/// cannot be read off them. A key store that keeps its keys under such names is migrated for the scopes listed here.
/// </remarks>
public class LegacyKeyScope
{
    /// <summary>
    /// Gets the exact name of the event store.
    /// </summary>
    public string EventStore { get; init; } = string.Empty;

    /// <summary>
    /// Gets the exact name of the namespace.
    /// </summary>
    public string Namespace { get; init; } = string.Empty;
}
