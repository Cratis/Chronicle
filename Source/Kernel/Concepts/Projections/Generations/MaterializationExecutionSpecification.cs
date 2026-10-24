// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.Projections.Generations;

/// <summary>
/// Immutable, credential-free execution inputs. The catalog supplies their materialization and generation identity.
/// </summary>
/// <param name="EventSequence">The source event sequence.</param>
/// <param name="CanonicalContent">The complete canonical execution document.</param>
/// <param name="Revision">The versioned content digest.</param>
/// <param name="CanonicalizationVersion">The canonicalization protocol version.</param>
/// <param name="InterpretationVersion">The engine interpretation version.</param>
/// <param name="RegisteringVersion">The original registering software version.</param>
/// <param name="HasKnownHistoricalProvenance">Whether these inputs also describe historical execution.</param>
public sealed record MaterializationExecutionSpecification(
    EventSequenceId EventSequence,
    string CanonicalContent,
    MaterializationRevision Revision,
    int CanonicalizationVersion = 1,
    int InterpretationVersion = 1,
    string? RegisteringVersion = null,
    bool HasKnownHistoricalProvenance = true)
{
    /// <summary>
    /// Captures a version-one canonical document without retaining mutable definitions.
    /// </summary>
    /// <param name="source">The event sequence.</param>
    /// <param name="content">The canonical document.</param>
    /// <param name="registeringVersion">The registering software version.</param>
    /// <returns>The immutable execution specification.</returns>
    public static MaterializationExecutionSpecification Capture(EventSequenceId source, string content, string? registeringVersion = null) =>
        new(source, content, Digest(source, content), RegisteringVersion: registeringVersion);

    /// <summary>
    /// Refuses unknown interpretation versions or content that does not match its digest.
    /// </summary>
    /// <exception cref="InvalidMaterializationExecution">The version or content digest is invalid.</exception>
    public void EnsureValid()
    {
        if (CanonicalizationVersion != 1) throw new InvalidMaterializationExecution();
        if (InterpretationVersion != 1) throw new InvalidMaterializationExecution();
        if (string.IsNullOrWhiteSpace(CanonicalContent)) throw new InvalidMaterializationExecution();
        if (Revision != Digest(EventSequence, CanonicalContent)) throw new InvalidMaterializationExecution();
    }

    static MaterializationRevision Digest(EventSequenceId source, string content) =>
        new($"v1:{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"1\n1\n{source.Value}\n{content}")))}");
}
