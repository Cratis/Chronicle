// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Represents one planned fault or recovery action.
/// </summary>
/// <param name="At">Offset within the chaos window.</param>
/// <param name="Kind">The target and fault.</param>
/// <param name="Resume">Whether this action recovers the target.</param>
internal sealed record ScheduledFault(TimeSpan At, FaultKind Kind, bool Resume = false);
