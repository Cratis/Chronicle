// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Namespaces;

/// <summary>The lifecycle status reported by an authorized namespace lookup.</summary>
public enum NamespaceBindingStatus
{
    /// <summary>The identity is reserved but cannot yet be used.</summary>
    Preparing = 0,

    /// <summary>The namespace may issue identities to bound writers.</summary>
    Open = 1,

    /// <summary>Removal is in progress and new identities are unavailable.</summary>
    Closing = 2,

    /// <summary>Removal completed; lookup never recreates the namespace.</summary>
    Removed = 3,

    /// <summary>An authorized operator must reconcile the interrupted lifecycle.</summary>
    RecoveryRequired = 4
}
