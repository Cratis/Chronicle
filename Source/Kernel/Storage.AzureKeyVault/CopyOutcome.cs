// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// What happened to one entry under an earlier name.
/// </summary>
internal enum CopyOutcome
{
    /// <summary>
    /// The copy already exists with the same value.
    /// </summary>
    AlreadyMigrated = 0,

    /// <summary>
    /// The entry was copied, or would be in a dry run.
    /// </summary>
    Copied = 1,

    /// <summary>
    /// The entry was left because it conflicts with something.
    /// </summary>
    ConflictKept = 2,

    /// <summary>
    /// The entry was left because of its attributes or versions.
    /// </summary>
    Held = 3
}
