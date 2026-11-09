// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents a producer grant declared in configuration. See <see cref="Federation.Grants"/>.
/// </summary>
public class FederationGrantConfiguration
{
    /// <summary>
    /// Gets or inits the pinned client certificate of the peer, <c language="text">sha256:&lt;base64&gt;</c>.
    /// </summary>
    public string ClientPin { get; init; } = string.Empty;

    /// <summary>
    /// Gets or inits the local event store whose outbox the peer may read.
    /// </summary>
    public string EventStore { get; init; } = string.Empty;

    /// <summary>
    /// Gets or inits the namespaces the peer may read.
    /// </summary>
    public IEnumerable<string> Namespaces { get; init; } = ["Default"];

    /// <summary>
    /// Gets or inits whether the grant covers all public event types.
    /// </summary>
    public bool AllPublic { get; init; }

    /// <summary>
    /// Gets or inits the specific event types granted when <see cref="AllPublic"/> is false.
    /// </summary>
    public IEnumerable<string> EventTypes { get; init; } = [];
}
