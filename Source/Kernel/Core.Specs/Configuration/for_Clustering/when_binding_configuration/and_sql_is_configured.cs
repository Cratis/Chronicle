// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Configuration.for_Clustering.when_binding_configuration;

public class and_sql_is_configured : Specification
{
    ChronicleOptions _options;

    void Establish() => _options = new ChronicleOptions();

    void Because() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["clustering:type"] = "Sql" })
        .Build()
        .Bind(_options);

    [Fact] void should_bind_sql_membership() => _options.Clustering.Type.ShouldEqual(ClusteringType.Sql);
    [Fact] void should_append_sql_without_renumbering_existing_types() => ((int)ClusteringType.Sql).ShouldEqual(2);
}
