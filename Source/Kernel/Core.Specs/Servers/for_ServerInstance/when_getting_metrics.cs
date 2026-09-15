// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Servers;

namespace Cratis.Chronicle.Servers.for_ServerInstance;

public class when_getting_metrics : Specification
{
    ServerInstance _serverInstance = default!;
    ServerInstanceMetrics _result = default!;

    void Establish() => _serverInstance = new ServerInstance();

    async Task Because() => _result = await _serverInstance.GetMetrics();

    [Fact] void should_report_a_cpu_usage_percentage_within_bounds() =>
        (_result.CpuUsagePercentage is >= 0 and <= 100).ShouldBeTrue();

    [Fact] void should_report_the_process_working_set() => (_result.WorkingSetBytes > 0).ShouldBeTrue();
}
