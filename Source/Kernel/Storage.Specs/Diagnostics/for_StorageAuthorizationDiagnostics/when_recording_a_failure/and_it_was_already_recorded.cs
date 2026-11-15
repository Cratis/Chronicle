// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Diagnostics.for_StorageAuthorizationDiagnostics.when_recording_a_failure;

public class and_it_was_already_recorded : given.a_registry
{
    bool _recorded;

    void Establish() => _registry.Record(_failure);
    void Because() => _recorded = _registry.Record(new("System", "EventTypeStatistics", "listIndexes"));

    [Fact] void should_not_report_it_as_new() => _recorded.ShouldBeFalse();
    [Fact] void should_hold_one_failure() => _registry.Failures.ShouldContainOnly(_failure);
    [Fact] void should_log_only_once() => _logger.Messages(LogLevel.Error).Count().ShouldEqual(1);
}
