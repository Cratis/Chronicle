// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Namespaces;

/// <summary>Signals refusal before any protected write committed, with a typed marker diagnostic.</summary>
/// <param name="reason">The marker diagnostic, never a replacement identity.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Storage.Namespaces.NamespaceCommitRejected")]
public sealed class NamespaceCommitRejected(NamespaceCommitRejectionReason reason) : Exception("The expected namespace incarnation is not open.")
{
    /// <summary>Initializes a refusal without an available marker diagnostic, preserving the existing constructor.</summary>
    public NamespaceCommitRejected() : this(NamespaceCommitRejectionReason.Unavailable)
    {
    }

    /// <summary>Gets the reason admission was refused.</summary>
    [Id(0)]
    public NamespaceCommitRejectionReason Reason { get; } = reason;

    /// <summary>Classifies the marker observed in the refusing transaction without admitting a write.</summary>
    /// <param name="marker">The marker, if present.</param>
    /// <param name="expected">The supplied incarnation.</param>
    /// <returns>The typed refusal, never an instruction to refresh the identity.</returns>
    public static NamespaceCommitRejected ForMarker(NamespaceMarker? marker, Concepts.NamespaceIncarnationId expected) => new(marker switch
    {
        null => NamespaceCommitRejectionReason.Unavailable,
        _ when marker.Incarnation != expected => NamespaceCommitRejectionReason.IncarnationMismatch,
        { IsOpen: false } => NamespaceCommitRejectionReason.Closing,
        _ => NamespaceCommitRejectionReason.Unavailable
    });
}
