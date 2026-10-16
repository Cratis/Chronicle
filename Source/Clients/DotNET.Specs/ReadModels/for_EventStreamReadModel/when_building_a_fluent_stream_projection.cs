// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.ReadModels.for_EventStreamReadModel;

public class when_building_a_fluent_stream_projection : given.stream_models
{
    ProjectionDefinition _definition;

    void Because()
    {
        var builder = Fluent<MonthlyWithdrawals>();
        builder.From<MoneyWithdrawn>();
        _definition = builder.Build();
    }

    [Fact] void should_imply_passive_for_a_fluent_projection() => _definition.IsActive.ShouldBeFalse();
    [Fact] void should_keep_the_source_key() => _definition.From.Values.Single().Key.ShouldEqual("$eventSourceId");
}
