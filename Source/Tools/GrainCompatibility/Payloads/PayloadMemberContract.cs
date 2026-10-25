// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

/// <summary>
/// A serialized payload member.
/// </summary>
/// <param name="Name">The serialized name, or the CLR name for Orleans members.</param>
/// <param name="Type">The displayed type, or the numeric value for enum members.</param>
/// <param name="Id">The Orleans serializer ID, when applicable.</param>
/// <param name="Level">The declaring inheritance level, with zero identifying the inventoried type.</param>
/// <param name="Constructor">Whether the member belongs to the primary-constructor wire segment.</param>
public record PayloadMemberContract(string Name, string Type, uint? Id = null, int Level = 0, bool Constructor = false);
