// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ensuring_indexes;

[Collection(SqlServerCollection.Name)]
public class and_online_builds_are_not_supported(SqlServerFixture fixture) : when_reconciling_a_changed_definition
{
    OnlineIndexGate _gate;

    protected override ISqlSinkHarness CreateHarness(OnlineIndexGate gate)
    {
        _gate = gate;
        gate.ForceOffline = true;
        return new SqlServerSinkHarness { Fixture = fixture, Interceptors = [gate] };
    }

    [Fact] void should_fall_back_to_a_normal_background_build() => _gate.UsedOfflineBuild.ShouldBeTrue();
}
