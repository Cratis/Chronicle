// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Grpc.AspNetCore.Server;

namespace Cratis.Chronicle.Server.for_GrpcServiceRegistrations.when_adding_grpc_services;

public class with_configured_sizes : Specification
{
    GrpcServiceOptions _result;

    void Because() => _result = given.the_grpc_options.For(new ChronicleOptions { MaxReceiveMessageSize = 200 * 1024 * 1024, MaxSendMessageSize = 50 * 1024 * 1024 });

    [Fact] void should_use_the_configured_receive_size() => _result.MaxReceiveMessageSize.ShouldEqual(200 * 1024 * 1024);
    [Fact] void should_use_the_configured_send_size() => _result.MaxSendMessageSize.ShouldEqual(50 * 1024 * 1024);
}
