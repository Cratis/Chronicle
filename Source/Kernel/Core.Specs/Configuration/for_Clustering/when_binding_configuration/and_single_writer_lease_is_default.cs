// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_Clustering.when_binding_configuration;

public class and_single_writer_lease_is_default : Specification
{
    readonly Clustering _options = new();

    [Fact] void should_select_automatic_enablement() => _options.SingleWriterLease.Enabled.ShouldBeNull();
    [Fact] void should_use_a_thirty_second_ttl() => _options.SingleWriterLease.Ttl.ShouldEqual(TimeSpan.FromSeconds(30));
    [Fact] void should_wait_two_minutes() => _options.SingleWriterLease.AcquireTimeout.ShouldEqual(TimeSpan.FromMinutes(2));
    [Fact] void should_apply_to_localhost_persistent_storage() => _options.SingleWriterLease.AppliesTo(ClusteringType.Localhost, "PostgreSql").ShouldBeTrue();
    [Fact] void should_not_apply_to_in_memory_storage() => _options.SingleWriterLease.AppliesTo(ClusteringType.Localhost, "InMemory").ShouldBeFalse();
    [Fact] void should_not_apply_to_mongodb_clustering() => _options.SingleWriterLease.AppliesTo(ClusteringType.MongoDB, "MongoDB").ShouldBeFalse();
}
