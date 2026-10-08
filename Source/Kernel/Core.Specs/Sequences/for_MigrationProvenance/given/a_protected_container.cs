// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Sequences.for_MigrationProvenance.given;

public class a_protected_container : Specification
{
    protected JsonObject _input;
    protected JsonObject _operations;
    protected bool _result;

    void Establish() => _input = new JsonObject { ["container"] = $"{VerificationMarkers.NewPrefix()}0" };
}
