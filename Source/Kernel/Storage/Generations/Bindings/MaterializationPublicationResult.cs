// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Generations.Bindings;

/// <summary>
/// Represents the materialization publication result.
/// </summary>
public enum MaterializationPublicationResult
{
    /// <summary>
    /// Published.
    /// </summary>
    Published = 0,

    /// <summary>
    /// UnknownMaterialization.
    /// </summary>
    UnknownMaterialization = 1,

    /// <summary>
    /// StaleBinding.
    /// </summary>
    StaleBinding = 2,

    /// <summary>
    /// CandidateNotFollowing.
    /// </summary>
    CandidateNotFollowing = 3,

    /// <summary>
    /// ReceiptConflict.
    /// </summary>
    ReceiptConflict = 4
}
