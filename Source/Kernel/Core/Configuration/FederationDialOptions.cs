// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the configuration of this kernel as a dialer of a hosted federation authority: the kernel connects out with the
/// certificate it obtained by enrolling, and one connection carries both directions.
/// </summary>
/// <remarks>
/// Dial-only mode is <see cref="Enabled"/> without <see cref="Federation.Enabled"/>: the kernel opens no listener, needs no listener
/// certificate and no open port. It is off by default.
/// </remarks>
public class FederationDialOptions
{
    /// <summary>
    /// Gets or inits whether this kernel dials the authority it enrolled with. Defaults to false.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Gets or inits the local event store whose inbox receives the authority's events, and whose outbox is served to the authority.
    /// </summary>
    public string EventStore { get; init; } = string.Empty;

    /// <summary>
    /// Gets or inits the local namespace. Defaults to Default.
    /// </summary>
    public string Namespace { get; init; } = "Default";

    /// <summary>
    /// Gets or inits the logical source name local consumer code refers to the authority by, in its event store subscriptions. Defaults to hosted.
    /// </summary>
    public string LogicalSource { get; init; } = "hosted";

    /// <summary>
    /// Gets or inits whether this kernel also serves its own public outbox to the authority over the same connection. Defaults to false.
    /// What the authority accepts is still decided by its policy for this kernel.
    /// </summary>
    public bool Serve { get; init; }

    /// <summary>
    /// Gets or inits the event types served to the authority. Empty serves every public event type.
    /// </summary>
    public IEnumerable<string> ServeEventTypes { get; init; } = [];
}
