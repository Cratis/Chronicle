// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Namespaces;

/// <summary>Literal, version-stable namespace failure codes carried by result envelopes, never transport statuses.</summary>
public static class NamespaceErrorCodes
{
    /// <summary>The namespace is closing.</summary>
    public const string Closing = "NamespaceClosing";

    /// <summary>The namespace is being initialized.</summary>
    public const string Initializing = "NamespaceInitializing";

    /// <summary>Operator recovery is required.</summary>
    public const string RecoveryRequired = "NamespaceRecoveryRequired";

    /// <summary>The supplied identity differs from the installed identity.</summary>
    public const string IncarnationMismatch = "NamespaceIncarnationMismatch";

    /// <summary>The supplied incarnation no longer admits writes.</summary>
    public const string IncarnationClosed = "NamespaceIncarnationClosed";

    /// <summary>The namespace identity or provider capability is unavailable.</summary>
    public const string IncarnationUnavailable = "NamespaceIncarnationUnavailable";

    /// <summary>The calling activation must reload its state.</summary>
    public const string IncarnationRequiresReactivation = "NamespaceIncarnationRequiresReactivation";

    /// <summary>A conditional namespace write conflicted.</summary>
    public const string WriteConflict = "NamespaceWriteConflict";

    /// <summary>Removal cannot safely be aborted.</summary>
    public const string RemovalCannotBeAborted = "NamespaceRemovalCannotBeAborted";
}
