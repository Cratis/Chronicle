// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBCapabilityProbe;

public class when_canceled_before_probing : Specification
{
    MongoDBCapabilityProbe _probe;
    Exception? _error;
    int _operations;

    void Establish() => _probe = new(
        _ =>
        {
            _operations++;
            return Task.CompletedTask;
        },
        _ =>
        {
            _operations++;
            return Task.CompletedTask;
        });
    async Task Because() => _error = await Catch.Exception(() => _probe.Probe(new CancellationToken(true)));

    [Fact] void should_propagate_cancellation() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_run_either_operation() => _operations.ShouldEqual(0);
}
