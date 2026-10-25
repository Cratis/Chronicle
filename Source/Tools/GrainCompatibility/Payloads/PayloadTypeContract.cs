// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

/// <summary>
/// The serialized shape of a type reachable from a grain call.
/// </summary>
/// <param name="Identity">The alias for aliased Orleans types, otherwise the displayed CLR name.</param>
/// <param name="ClrName">The displayed CLR name.</param>
/// <param name="Kind">The serialized representation.</param>
/// <param name="Alias">The Orleans alias, when present.</param>
/// <param name="Underlying">The underlying type of a concept or enum.</param>
/// <param name="Members">The serialized members.</param>
public record PayloadTypeContract(
    string Identity,
    string ClrName,
    PayloadKind Kind,
    string? Alias,
    string? Underlying,
    IReadOnlyList<PayloadMemberContract> Members);
