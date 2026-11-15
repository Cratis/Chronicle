// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Services.for_ConnectionCallers;

public class an_authenticated_request_with_a_subject : Specification
{
    ConnectionCaller _result;

    void Because() => _result = ConnectionCallers.From(AuthenticatedCalls.As("alice"), new ChronicleOptions { Authentication = new() { Enabled = true } });

    [Fact] void should_be_owned_by_the_subject() => _result.Owner.ShouldEqual(AuthenticatedCalls.OwnerOf("alice"));
    [Fact] void should_carry_the_principal() => _result.Principal.ShouldNotBeNull();
}
