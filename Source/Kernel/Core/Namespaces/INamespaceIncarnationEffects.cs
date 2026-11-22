// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces;

/// <summary>Defines the durable owner's effects, separate from namespace admission.</summary>
public interface INamespaceIncarnationEffects
{
    /// <summary>Installs the durable recovery reminder before pending control state is committed.</summary>
    /// <returns>The durable reminder acknowledgement.</returns>
    Task EnsureWake();

    /// <summary>Records the creation notification, then commits the namespace listing and publishes its notification.</summary>
    /// <param name="state">The reserved identity; repeated notifications must be safe after a lost acknowledgement.</param>
    /// <returns>The acknowledged creation effects.</returns>
    Task Create(NamespaceLifecycle state);

    /// <summary>Executes one already-recorded destructive checkpoint, never marker close.</summary>
    /// <param name="state">The persisted removal intent.</param>
    /// <returns>The acknowledged checkpoint completion.</returns>
    Task Remove(NamespaceLifecycle state);
}
