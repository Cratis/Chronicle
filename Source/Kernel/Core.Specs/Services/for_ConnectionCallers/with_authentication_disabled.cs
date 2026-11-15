// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Services.for_ConnectionCallers;

public class with_authentication_disabled : Specification
{
    ConnectionCaller _result;

    void Because() => _result = ConnectionCallers.From(AuthenticatedCalls.Unauthenticated(), new ChronicleOptions { Authentication = new() { Enabled = false } });

    [Fact] void should_be_the_anonymous_owner() => _result.Owner.ShouldEqual(ConnectionOwner.Anonymous);
}
