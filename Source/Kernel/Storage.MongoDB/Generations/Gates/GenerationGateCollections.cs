// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Gates;

/// <summary>
/// Defines the gate fields shared with writer fencing.
/// </summary>
internal static class GenerationGateCollections
{
    /// <summary>
    /// The writer gate collection.
    /// </summary>
    internal const string Gates = "fence-gates";

    /// <summary>
    /// The revocation epoch.
    /// </summary>
    internal const string Epoch = "epoch";

    /// <summary>
    /// The writer admission counter.
    /// </summary>
    internal const string Admitted = "admitted";

    /// <summary>
    /// The epoch most recently issued by an owner grant; revocation leaves it behind.
    /// </summary>
    internal const string Granted = "granted";
}
