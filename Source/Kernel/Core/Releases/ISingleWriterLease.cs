// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clustering;

namespace Cratis.Chronicle.SingleWriter;

/// <summary>
/// Exposes this process's single-writer lease state.
/// </summary>
public interface ISingleWriterLease
{
    /// <summary>
    /// Gets whether this process still holds its lease.
    /// </summary>
    bool IsHeld { get; }

    /// <summary>
    /// Gets the unique identity of this process lifetime.
    /// </summary>
    SingleWriterLeaseOwner Owner { get; }
}
