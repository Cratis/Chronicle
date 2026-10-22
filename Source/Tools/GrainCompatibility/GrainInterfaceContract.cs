// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrainCompatibility;

/// <summary>
/// A grain interface as recorded in an OrleansContracts.txt.
/// </summary>
/// <param name="InterfaceType">The grain interface type Orleans identifies the interface by on the wire.</param>
/// <param name="ClrName">The CLR name of the interface.</param>
/// <param name="Version">The interface version, from its [Version] attribute; 0 when it has none.</param>
/// <param name="Retired">Whether the interface has been removed and is kept only as history.</param>
/// <param name="Methods">The methods, by the wire identity Orleans uses for each.</param>
public record GrainInterfaceContract(
    string InterfaceType,
    string ClrName,
    int Version,
    bool Retired,
    IReadOnlyDictionary<string, GrainMethodContract> Methods);
