// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Grpc.AspNetCore.Server;

namespace Cratis.Chronicle.Server.for_GrpcServiceRegistrations.when_adding_grpc_services;

/// <summary>
/// The clients allow 100 MB by default, so a kernel left on ASP.NET Core's 4 MB turns every large append into ResourceExhausted (#4821).
/// </summary>
public class with_default_options : Specification
{
    GrpcServiceOptions _result;

    void Because() => _result = given.the_grpc_options.For(new ChronicleOptions());

    [Fact] void should_accept_what_the_clients_default_to_sending() => _result.MaxReceiveMessageSize.ShouldEqual(100 * 1024 * 1024);
    [Fact] void should_not_limit_what_it_sends() => _result.MaxSendMessageSize.ShouldBeNull();
}
