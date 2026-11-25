// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterReconnectDiagnostics;

public class when_describing_targets : Specification
{
    string _description;

    void Because() => _description = ClusterReconnectDiagnostics.DescribeTargets("""{"Targets":[{"ConnectionId":"the-connection","Silo":"the-silo"}]}""");

    [Fact] void should_name_the_connection() => _description.ShouldContain("ConnectionId=the-connection");
    [Fact] void should_name_the_silo() => _description.ShouldContain("Silo=the-silo");
}
