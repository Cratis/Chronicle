// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Services.for_ConnectionCallers;

public class a_call_without_a_server_context_and_authentication_enabled : Specification
{
    ConnectionCaller _result;

    void Because() => _result = ConnectionCallers.From(default, new ChronicleOptions { Authentication = new() { Enabled = true } });

    [Fact] void should_be_the_in_process_owner() => _result.Owner.ShouldEqual(ConnectionOwner.InProcess);
}
