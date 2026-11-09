// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBCapabilityProbe;

public class when_canceled_during_a_probe : Specification
{
    CancellationTokenSource _cancellation;
    MongoDBCapabilityProbe _probe;
    Exception? _error;
    bool _collationProbed;

    void Establish()
    {
        _cancellation = new();
        _probe = new(
            token =>
            {
                _cancellation.Cancel();
                token.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            },
            _ => { _collationProbed = true; return Task.CompletedTask; });
    }

    async Task Because() => _error = await Catch.Exception(() => _probe.Probe(_cancellation.Token));

    [Fact] void should_propagate_cancellation() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_probe_collation() => _collationProbed.ShouldBeFalse();

    void Destroy() => _cancellation.Dispose();
}
