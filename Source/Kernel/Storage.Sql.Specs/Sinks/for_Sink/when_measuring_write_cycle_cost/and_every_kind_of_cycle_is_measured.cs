// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_measuring_write_cycle_cost;

/// <summary>
/// Applies events of every kind of write cycle and records what each costs.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_every_kind_of_cycle_is_measured<THarness> : given.a_cost_measurement<THarness>
    where THarness : ISqlSinkHarness
{
    Task Because() => MeasureEveryKind();

    [Fact] public void should_measure_every_kind() => Measurements.Select(measurement => measurement.Kind).Distinct().Count().ShouldEqual(Kinds.Count);
    [Fact] public void should_issue_commands_for_every_measurement() => Measurements.ToList().TrueForAll(measurement => measurement.Commands > 0).ShouldBeTrue();
}
