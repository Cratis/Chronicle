// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Configuration.for_Clustering.when_binding_configuration;

public class and_single_writer_lease_is_configured : Specification
{
    ChronicleOptions _options;

    void Because() => _options = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["clustering:singleWriterLease:enabled"] = "false",
        ["clustering:singleWriterLease:ttl"] = "00:01:00",
        ["clustering:singleWriterLease:acquireTimeout"] = "00:00:10",
        ["clustering:singleWriterLease:standby"] = "true",
        ["clustering:singleWriterLease:standbyStartupTimeout"] = "00:20:00"
    }).Build().Get<ChronicleOptions>()!;

    [Fact] void should_bind_enabled() => _options.Clustering.SingleWriterLease.Enabled.ShouldEqual(false);
    [Fact] void should_bind_ttl() => _options.Clustering.SingleWriterLease.Ttl.ShouldEqual(TimeSpan.FromMinutes(1));
    [Fact] void should_bind_standby() => _options.Clustering.SingleWriterLease.Standby.ShouldBeTrue();
    [Fact] void should_bind_standby_startup_timeout() => _options.Clustering.SingleWriterLease.StandbyStartupTimeout.ShouldEqual(TimeSpan.FromMinutes(20));
    [Fact] void should_bind_acquire_timeout() => _options.Clustering.SingleWriterLease.AcquireTimeout.ShouldEqual(TimeSpan.FromSeconds(10));
}
