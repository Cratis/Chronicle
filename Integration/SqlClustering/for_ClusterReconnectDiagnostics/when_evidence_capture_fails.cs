// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterReconnectDiagnostics;

public class when_evidence_capture_fails : Specification
{
    readonly TimeoutException _original = new("Missed original reconnect deadline");
    Exception? _error;

    async Task Because() => _error = await Catch.Exception(async () =>
    {
        await ClusterReconnectDiagnostics.Capture(() => throw new InvalidOperationException("Evidence unavailable"));
        throw _original;
    });

    [Fact] void should_preserve_the_frozen_timeout() => _error.ShouldEqual(_original);
    [Fact] void should_preserve_its_message() => _error!.Message.ShouldEqual("Missed original reconnect deadline");
}
