// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Namespaces;

/// <summary>The observed reason a namespace fence refused admission before writing events or receipts.</summary>
public enum NamespaceCommitRejectionReason
{
    /// <summary>No initialized namespace marker is available.</summary>
    Unavailable = 0,

    /// <summary>The installed marker belongs to another incarnation.</summary>
    IncarnationMismatch = 1,

    /// <summary>The expected incarnation's marker is closed.</summary>
    Closing = 2
}
