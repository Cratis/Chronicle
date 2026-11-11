// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Represents a silo that is up and a member of the cluster, but answered a call too late.
/// </summary>
/// <param name="Address">The address of the silo, as it appears in the call that timed out.</param>
/// <param name="IsThisSilo">Whether the silo is the one making the call.</param>
public record BusySilo(string Address, bool IsThisSilo);
