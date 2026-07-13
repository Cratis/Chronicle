// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_parsing_url_with_load_balancer_option : Specification
{
    ChronicleConnectionStringBuilder _builder;

    void Establish() => _builder = new ChronicleConnectionStringBuilder("chronicle://host1,host2/?loadBalancer=random");

    [Fact] void should_have_load_balancer() => _builder.LoadBalancer.ShouldEqual("random");
    [Fact] void should_include_load_balancer_when_building() => _builder.Build().ShouldEqual("chronicle://host1:35000,host2:35000?loadBalancer=random");
}
