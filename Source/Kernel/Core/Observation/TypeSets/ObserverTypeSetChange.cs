// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.TypeSets;

/// <summary>
/// Represents the pending membership intent.
/// </summary>
/// <param name="Revision">The revision the change will publish.</param>
/// <param name="Started">When the change began.</param>
/// <param name="InitialTail">The committed sequence tail when the intent was first written.</param>
/// <param name="Additions">The new member identifiers.</param>
/// <param name="Retirements">The retirement requests to recheck at commit.</param>
public record ObserverTypeSetChange(long Revision, DateTimeOffset Started, ulong InitialTail, IEnumerable<string> Additions, IEnumerable<ObserverEventTypeRetirementRequest> Retirements);
