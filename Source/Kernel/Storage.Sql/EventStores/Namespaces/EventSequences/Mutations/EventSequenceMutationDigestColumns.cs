// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Mutations;

/// <summary>
/// Converts event sequence mutation digests to and from their canonical hex-string column representation.
/// </summary>
/// <remarks>
/// <see cref="EventSequenceMutationDefinitionDigestV1"/> and <see cref="EventSequenceMutationReceiptDigestV1"/>
/// are not <c>ConceptAs&lt;T&gt;</c> types, so the automatic concept value-conversion Cratis.Arc.EntityFrameworkCore
/// wires up does not apply to them - they need an explicit <c>HasConversion</c> in <see cref="NamespaceDbContext"/>.
/// Their constructors accept a <see cref="ReadOnlySpan{T}"/>, which cannot appear inside an EF Core conversion
/// expression tree (ref struct parameters are not supported there), so the conversion goes through these plain
/// methods instead - each is a normal method call from the expression tree's perspective, with the span usage
/// hidden inside an ordinary method body. Both digests are 32 bytes, so the hex form is always 64 characters,
/// matching the column width already used for <c>ActiveCommandHash</c>/<c>CommandHash</c>.
/// </remarks>
static class EventSequenceMutationDigestColumns
{
    /// <summary>
    /// Converts a version 1 definition digest to its canonical hex-string column representation.
    /// </summary>
    /// <param name="digest">The definition digest to convert.</param>
    /// <returns>The hex-string representation.</returns>
    internal static string DefinitionDigestToHex(EventSequenceMutationDefinitionDigestV1 digest) => Convert.ToHexString(digest.Snapshot());

    /// <summary>
    /// Converts a hex-string column value back to a version 1 definition digest.
    /// </summary>
    /// <param name="hex">The hex-string representation to convert.</param>
    /// <returns>The definition digest.</returns>
    internal static EventSequenceMutationDefinitionDigestV1 DefinitionDigestFromHex(string hex) => new(Convert.FromHexString(hex));

    /// <summary>
    /// Converts a version 1 receipt digest to its canonical hex-string column representation.
    /// </summary>
    /// <param name="digest">The receipt digest to convert.</param>
    /// <returns>The hex-string representation.</returns>
    internal static string ReceiptDigestToHex(EventSequenceMutationReceiptDigestV1 digest) => Convert.ToHexString(digest.Snapshot());

    /// <summary>
    /// Converts a hex-string column value back to a version 1 receipt digest.
    /// </summary>
    /// <param name="hex">The hex-string representation to convert.</param>
    /// <returns>The receipt digest.</returns>
    internal static EventSequenceMutationReceiptDigestV1 ReceiptDigestFromHex(string hex) => new(Convert.FromHexString(hex));
}
