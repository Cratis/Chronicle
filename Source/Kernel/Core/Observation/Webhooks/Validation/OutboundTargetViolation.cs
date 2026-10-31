// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.Validation;

/// <summary>
/// Identifies the destination policy violation independently of its rollout mode.
/// </summary>
internal enum OutboundTargetViolation
{
    /// <summary>Cloud metadata requires explicit approval in every mode.</summary>
    Metadata = 0,

    /// <summary>The destination belongs to a local address range.</summary>
    Local = 1,

    /// <summary>The destination belongs to another private or special range.</summary>
    Private = 2,

    /// <summary>The destination violates scheme, host, or resolution policy.</summary>
    Policy = 3
}
