// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

/// <summary>
/// A serialized payload member.
/// </summary>
/// <param name="Name">The serialized name, or the CLR name for Orleans members.</param>
/// <param name="Type">The displayed type, or the numeric value for enum members.</param>
/// <param name="Id">The Orleans serializer ID, when applicable.</param>
public record PayloadMemberContract(string Name, string Type, int? Id = null);
