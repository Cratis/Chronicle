// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Generations.Gates;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Holds the names of the collections and fields the writer fence keeps its records in.
/// </summary>
public static class FenceCollections
{
    /// <summary>
    /// The collection holding one gate record per materialization generation.
    /// </summary>
    public const string Gates = GenerationGateCollections.Gates;

    /// <summary>
    /// The collection holding one receipt per committed fenced batch.
    /// </summary>
    public const string Receipts = "fence-receipts";

    /// <summary>
    /// The gate record field holding the owner epoch.
    /// </summary>
    public const string Epoch = GenerationGateCollections.Epoch;

    /// <summary>
    /// The gate record field counting admitted batches; every admission writes it, so admission conflicts with revocation.
    /// </summary>
    public const string Admitted = GenerationGateCollections.Admitted;

    /// <summary>
    /// The gate record field counting unknown-commit resolutions; a resolution writes it to close the gate on the batch.
    /// </summary>
    public const string Resolutions = "resolutions";

    /// <summary>
    /// The receipt field naming the gate the batch was admitted through.
    /// </summary>
    public const string Gate = "gate";

    /// <summary>
    /// The receipt field holding the time the receipt was written, used for expiring old receipts.
    /// </summary>
    public const string At = "at";
}
