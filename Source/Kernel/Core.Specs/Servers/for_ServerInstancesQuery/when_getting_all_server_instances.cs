// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Servers;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Servers.for_ServerInstancesQuery;

public class when_getting_all_server_instances : Specification
{
    SiloAddress _firstSilo = default!;
    SiloAddress _secondSilo = default!;
    IServerInstance _firstServerInstance = default!;
    IServerInstance _secondServerInstance = default!;
    ServerInstancesQuery _query = default!;
    ServerInstanceDetails[] _result = default!;

    void Establish()
    {
        _firstSilo = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1);
        _secondSilo = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11112), 2);

        _firstServerInstance = Substitute.For<IServerInstance>();
        _firstServerInstance.GetMetrics().Returns(new ServerInstanceMetrics { CpuUsagePercentage = 12.5, WorkingSetBytes = 1_000 });

        _secondServerInstance = Substitute.For<IServerInstance>();
        _secondServerInstance.GetMetrics().Returns(new ServerInstanceMetrics { CpuUsagePercentage = 42, WorkingSetBytes = 2_000 });

        var management = Substitute.For<IManagementGrain>();
        management.GetHosts(true).Returns(new Dictionary<SiloAddress, SiloStatus>
        {
            [_firstSilo] = SiloStatus.Active,
            [_secondSilo] = SiloStatus.Active
        });

        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IManagementGrain>(Arg.Any<long>(), Arg.Any<string>()).Returns(management);
        grainFactory.GetGrain<IServerInstance>(_firstSilo.ToParsableString(), Arg.Any<string>()).Returns(_firstServerInstance);
        grainFactory.GetGrain<IServerInstance>(_secondSilo.ToParsableString(), Arg.Any<string>()).Returns(_secondServerInstance);

        _query = new ServerInstancesQuery(grainFactory, Options.Create(new ChronicleOptions()));
    }

    async Task Because() => _result = [.. await _query.GetAll()];

    [Fact] void should_return_one_instance_per_silo() => _result.Length.ShouldEqual(2);

    [Fact] void should_return_the_address_status_cpu_and_memory_for_each_silo() => _result.ShouldContainOnly(
        new ServerInstanceDetails(_firstSilo.ToParsableString(), _firstSilo.ToParsableString(), "Active", 12.5, 1_000),
        new ServerInstanceDetails(_secondSilo.ToParsableString(), _secondSilo.ToParsableString(), "Active", 42, 2_000));
}
