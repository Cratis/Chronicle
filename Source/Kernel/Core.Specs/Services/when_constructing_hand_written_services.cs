// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Cratis.Chronicle.Services.given.hand_written_routing_surface;

namespace Cratis.Chronicle.Services;

public class when_constructing_hand_written_services : Specification
{
    public static TheoryData<string> Services()
    {
        var services = new TheoryData<string>();
        foreach (var type in HandWritten)
        {
            services.Add(type.FullName!);
        }

        return services;
    }

    [Theory]
    [MemberData(nameof(Services))]
    void should_construct_the_service(string serviceName)
    {
        var type = HandWritten.Single(type => type.FullName == serviceName);
        Create(type).GetType().ShouldEqual(type);
    }
}
