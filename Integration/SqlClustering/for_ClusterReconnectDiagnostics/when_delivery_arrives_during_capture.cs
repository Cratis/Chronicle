// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterReconnectDiagnostics;

public class when_delivery_arrives_during_capture : Specification
{
    bool _delivered;
    Exception? _error;

    async Task Because() => _error = await Catch.Exception(async () =>
    {
        var failure = new TimeoutException($"Delivered at deadline={_delivered}");
        await ClusterReconnectDiagnostics.Capture(() =>
        {
            _delivered = true;
            return Task.CompletedTask;
        });
        throw failure;
    });

    [Fact] void should_have_delivered_during_capture() => _delivered.ShouldBeTrue();
    [Fact] void should_still_fail_with_the_deadline_snapshot() => _error!.Message.ShouldEqual("Delivered at deadline=False");
}
