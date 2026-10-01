// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Setup.Serialization;
using Cratis.Chronicle.Storage;
using Cratis.Orleans;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts;

public class when_round_tripping_snapshot_through_orleans : Specification
{
    ObserverAlertSnapshot _original;
    ObserverAlertSnapshot _result;

    void Establish() => _original = new(new("observer", "store", "namespace", EventSequenceId.Log), [new(FailedPartitionId.New(), "partition", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddMinutes(1), 3, true, FailureKind.Handling, "Failed")], true, false, 10)
    {
        PartitionsEndedAs = AlertClearedReason.Cleared,
        QuarantineEndedAs = AlertClearedReason.Revived
    };

    void Because()
    {
        var services = new ServiceCollection();
        services.AddCratisOrleansSerializers();
        services.AddSingleton(Substitute.For<IExpandoObjectConverter>());
        services.AddSingleton(Substitute.For<IStorage>());
        var builder = Substitute.For<ISiloBuilder>();
        builder.Services.Returns(services);
        builder.ConfigureSerialization();
        using var provider = services.BuildServiceProvider();
        var serializer = provider.GetRequiredService<Serializer>();
        _result = serializer.Deserialize<ObserverAlertSnapshot>(serializer.SerializeToArray(_original));
    }

    [Fact] void should_keep_the_observer_key() => _result.Observer.ShouldEqual(_original.Observer);
    [Fact] void should_keep_the_failure_evidence() => _result.FailedPartitions.ShouldContainOnly(_original.FailedPartitions.Single());
    [Fact] void should_keep_the_quarantine_state() => _result.IsQuarantined.ShouldBeTrue();
    [Fact] void should_keep_the_retry_limit() => _result.MaxRetryAttempts.ShouldEqual(10);
    [Fact] void should_keep_the_partition_clear_reason() => _result.PartitionsEndedAs.ShouldEqual(AlertClearedReason.Cleared);
    [Fact] void should_keep_the_quarantine_clear_reason() => _result.QuarantineEndedAs.ShouldEqual(AlertClearedReason.Revived);
}
