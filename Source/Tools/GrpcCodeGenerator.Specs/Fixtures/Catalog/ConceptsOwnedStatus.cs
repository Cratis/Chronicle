// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.SharedTypeCatalog;

/// <summary>
/// Stands in for a type reused from a Kernel project Core depends on, one namespace segment deeper than a
/// Core-declared type - <c>Concepts.Jobs.JobStatus</c> is the real shape this mirrors, and the whole reason
/// <see cref="Tools.GrpcCodeGenerator.SharedTypeRegistry"/> treats "Concepts" as a transparent layer segment.
/// </summary>
public enum ConceptsOwnedStatus
{
    /// <summary>Represents the only value.</summary>
    Only = 0,
}
