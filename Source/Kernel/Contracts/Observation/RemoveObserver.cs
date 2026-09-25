// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents the request for removing an observer and everything keyed to it.
/// </summary>
/// <remarks>
/// Removal is scoped to the whole event store, not to a single namespace: an observer's definition and, where it has
/// one, its projection definition are store-level records shared by every namespace, so removing them in one namespace
/// and leaving the namespaced state behind in the others would be the same inconsistency the removal exists to clear
/// up. <see cref="Namespace"/> therefore only selects the event store's namespace the caller happens to be working in;
/// the guard is evaluated across every namespace and the removal covers every namespace.
/// </remarks>
[ProtoContract]
public class RemoveObserver : IObserverCommand
{
    /// <inheritdoc/>
    [ProtoMember(1)]
    public string EventStore { get; set; }

    /// <inheritdoc/>
    [ProtoMember(2)]
    public string Namespace { get; set; }

    /// <inheritdoc/>
    [ProtoMember(3)]
    public string ObserverId { get; set; }

    /// <inheritdoc/>
    [ProtoMember(4)]
    public string EventSequenceId { get; set; } = string.Empty;
}
