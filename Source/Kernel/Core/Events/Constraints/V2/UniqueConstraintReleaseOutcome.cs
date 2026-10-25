// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.V2;

/// <summary>
/// Describes whether released components may participate in a claim or removal.
/// </summary>
internal enum UniqueConstraintReleaseOutcome
{
    /// <summary>
    /// The components are readable and may be passed to the encoder.
    /// </summary>
    Claim = 0,

    /// <summary>
    /// An affected component was erased; no claim or removal may be encoded.
    /// </summary>
    NoClaimErased = 1
}
