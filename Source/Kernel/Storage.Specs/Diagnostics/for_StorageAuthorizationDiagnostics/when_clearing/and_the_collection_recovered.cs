// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Diagnostics.for_StorageAuthorizationDiagnostics.when_clearing;

public class and_the_collection_recovered : given.a_registry
{
    void Establish() => _registry.Record(_failure);
    void Because() => _registry.Clear("System", "EventTypeStatistics");

    [Fact] void should_hold_no_failures() => _registry.Failures.ShouldBeEmpty();
    [Fact] void should_log_the_recovery() => _logger.Messages(LogLevel.Information).Count().ShouldEqual(1);
}
