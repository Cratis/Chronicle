// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// The result of looking for encryption keys stored under earlier names. Holds counts only, never identifiers.
/// </summary>
/// <param name="LayoutVerified">Whether the layout of the key store was confirmed to be free for the paths this version writes.</param>
/// <param name="LayoutConflict">Whether earlier material occupies the paths this version writes, so they cannot be used.</param>
/// <param name="EarlierNameKeys">The number of keys stored under earlier names. A lower bound unless <paramref name="Complete"/> is true.</param>
/// <param name="EarlierNameErasures">The number of erasure records stored under earlier names. A lower bound unless <paramref name="Complete"/> is true.</param>
/// <param name="UnresolvedShapes">The number of entries under earlier names that cannot be addressed unambiguously.</param>
/// <param name="Complete">Whether the whole key store was inspected.</param>
public record EncryptionKeyStoreUpgradeStatus(
    bool LayoutVerified,
    bool LayoutConflict,
    long EarlierNameKeys,
    long EarlierNameErasures,
    long UnresolvedShapes,
    bool Complete)
{
    /// <summary>
    /// Gets whether any material stored under earlier names was found.
    /// </summary>
    public bool EarlierMaterialPresent => EarlierNameKeys + EarlierNameErasures + UnresolvedShapes > 0;

    /// <summary>
    /// Gets whether the layout is unusable: it is in conflict, or it could not be verified.
    /// </summary>
    public bool LayoutUnusable => LayoutConflict || !LayoutVerified;
}
