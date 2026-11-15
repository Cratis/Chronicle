// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Diagnostics.for_StorageAuthorizationDiagnostics.when_recording_a_failure;

public class and_recording_concurrently : given.a_registry
{
    Task Because() => Task.WhenAll(Enumerable.Range(0, 100).Select(_ => Task.Run(() => _registry.Record(_failure))));

    [Fact] void should_hold_one_failure() => _registry.Failures.ShouldContainOnly(_failure);
    [Fact] void should_log_only_once() => _logger.Messages(LogLevel.Error).Count().ShouldEqual(1);
}
