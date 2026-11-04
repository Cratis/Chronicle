// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.given;

public class an_inventory : Specification
{
    protected PayloadContractManifest _manifest;

    void Establish() => _manifest = PayloadInventory.From([typeof(payload_types.IFixtureGrain)], _ => _ == typeof(payload_types.Custom));

    protected PayloadTypeContract Contract<T>() => _manifest.Types[PayloadTypeName.Of(typeof(T))];
}
