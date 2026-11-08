// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Generations.Enablement;

/// <summary>
/// How far projection-generation initialization has progressed for an event store, derived from durable receipts only.
/// </summary>
internal enum GenerationInitializationProgress
{
    /// <summary>
    /// No receipt exists; the store behaves as a legacy store.
    /// </summary>
    NotStarted = 0,

    /// <summary>
    /// Initialization has begun but no materialization has been adopted.
    /// </summary>
    Quiescing = 1,

    /// <summary>
    /// At least one materialization has been adopted in place.
    /// </summary>
    Adopted = 2
}
