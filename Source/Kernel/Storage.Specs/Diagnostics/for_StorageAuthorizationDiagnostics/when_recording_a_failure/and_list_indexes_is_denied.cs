// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Diagnostics.for_StorageAuthorizationDiagnostics.when_recording_a_failure;

public class and_list_indexes_is_denied : given.a_registry
{
    bool _recorded;

    void Because() => _recorded = _registry.Record(_failure);

    [Fact] void should_report_it_as_new() => _recorded.ShouldBeTrue();
    [Fact] void should_hold_the_failure() => _registry.Failures.ShouldContainOnly(_failure);
    [Fact] void should_log_one_error() => _logger.Messages(LogLevel.Error).Count().ShouldEqual(1);
    [Fact] void should_name_the_database_in_the_log() => _logger.Messages(LogLevel.Error).Single().ShouldContain("System");
    [Fact] void should_name_the_collection_in_the_log() => _logger.Messages(LogLevel.Error).Single().ShouldContain("EventTypeStatistics");
    [Fact] void should_name_the_operation_in_the_log() => _logger.Messages(LogLevel.Error).Single().ShouldContain("listIndexes");
    [Fact] void should_not_log_an_exception() => _logger.Exceptions.ShouldBeEmpty();
}
