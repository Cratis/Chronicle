// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Namespaces;

/// <summary>
/// The durable lifecycle of a namespace incarnation.
/// </summary>
public enum NamespaceLifecycleStatus
{
    /// <summary>The identity is reserved, but its local marker is not yet acknowledged.</summary>
    Preparing = 0,

    /// <summary>The local marker is installed and tokens may be issued.</summary>
    Open = 1,

    /// <summary>Removal is in progress and no tokens may be issued.</summary>
    Closing = 2,

    /// <summary>Removal completed. Only an explicit Ensure may allocate a new identity.</summary>
    Removed = 3,

    /// <summary>Automatic recovery stopped; an authorized operator must reconcile the operation.</summary>
    RecoveryRequired = 4
}
