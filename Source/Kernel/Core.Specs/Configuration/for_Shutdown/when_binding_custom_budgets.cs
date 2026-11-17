// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Configuration.for_Shutdown;

public class when_binding_custom_budgets : Specification
{
    Shutdown _shutdown;

    void Because() => _shutdown = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["DrainTimeout"] = "00:00:04",
        ["HostShutdownReserve"] = "00:00:07"
    }).Build().Get<Shutdown>()!;

    [Fact] void should_bind_the_drain_budget() => _shutdown.DrainTimeout.ShouldEqual(TimeSpan.FromSeconds(4));
    [Fact] void should_bind_the_host_stop_reserve() => _shutdown.HostShutdownReserve.ShouldEqual(TimeSpan.FromSeconds(7));
    [Fact] void should_add_the_budgets_for_host_shutdown() => _shutdown.HostShutdownTimeout.ShouldEqual(TimeSpan.FromSeconds(11));
}
