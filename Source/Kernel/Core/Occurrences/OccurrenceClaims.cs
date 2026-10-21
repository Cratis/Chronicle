// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Carries a bounded batch of durable leases over the generated command surface.
/// </summary>
/// <param name="Claims">The claimed occurrences.</param>
public record OccurrenceClaims(IEnumerable<OccurrenceClaimDetails> Claims);
