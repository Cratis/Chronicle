// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Services.for_ConnectionCallers;

public class an_unauthenticated_request : Specification
{
    ConnectionCaller _result;

    void Because() => _result = ConnectionCallers.From(AuthenticatedCalls.Unauthenticated(), new ChronicleOptions { Authentication = new() { Enabled = true } });

    [Fact] void should_be_unidentified() => _result.Owner.IsUnidentified.ShouldBeTrue();
}
