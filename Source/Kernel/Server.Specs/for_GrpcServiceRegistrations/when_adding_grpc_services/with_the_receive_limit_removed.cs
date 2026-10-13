// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Grpc.AspNetCore.Server;

namespace Cratis.Chronicle.Server.for_GrpcServiceRegistrations.when_adding_grpc_services;

public class with_the_receive_limit_removed : Specification
{
    GrpcServiceOptions _result;

    void Because() => _result = given.the_grpc_options.For(new ChronicleOptions { MaxReceiveMessageSize = null });

    [Fact] void should_not_limit_what_it_receives() => _result.MaxReceiveMessageSize.ShouldBeNull();
}
