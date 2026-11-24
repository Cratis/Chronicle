// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts.Namespaces;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces;

/// <summary>Recognizes typed namespace failures before an envelope loses their structure.</summary>
public static class NamespaceExceptionCodes
{
    /// <summary>Gets a stable code without parsing messages or exception type names.</summary>
    /// <param name="exception">The failure, possibly inside a recognized invocation or Orleans wrapper.</param>
    /// <returns>The literal code, or null for an unrelated, ambiguous, or unknown-outcome failure.</returns>
    public static string? For(Exception exception) => exception switch
    {
        NamespaceClosing => NamespaceErrorCodes.Closing,
        NamespaceInitializing => NamespaceErrorCodes.Initializing,
        NamespaceRecoveryRequired => NamespaceErrorCodes.RecoveryRequired,
        NamespaceIncarnationUnavailable or NamespaceCommitNotSupported => NamespaceErrorCodes.IncarnationUnavailable,
        NamespaceIncarnationRequiresReactivation => NamespaceErrorCodes.IncarnationRequiresReactivation,
        NamespaceLifecycleChanged => NamespaceErrorCodes.WriteConflict,
        NamespaceRemovalCannotBeAborted => NamespaceErrorCodes.RemovalCannotBeAborted,
        NamespaceCommitRejected { Reason: NamespaceCommitRejectionReason.IncarnationMismatch } => NamespaceErrorCodes.IncarnationMismatch,
        NamespaceCommitRejected { Reason: NamespaceCommitRejectionReason.Closing } => NamespaceErrorCodes.IncarnationClosed,
        NamespaceCommitRejected { Reason: NamespaceCommitRejectionReason.Unavailable } => NamespaceErrorCodes.IncarnationUnavailable,
        AggregateException { InnerExceptions.Count: 1 } aggregate => For(aggregate.InnerExceptions[0]),
        TargetInvocationException { InnerException: { } inner } => For(inner),
        OrleansException { InnerException: { } inner } => For(inner),
        _ => null
    };
}
