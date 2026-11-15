// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Diagnostics.for_StorageAuthorizationDiagnostics.when_clearing;

public class and_nothing_was_recorded : given.a_registry
{
    void Because() => _registry.Clear("System", "EventTypeStatistics");

    [Fact] void should_not_log() => _logger.Messages(LogLevel.Information).ShouldBeEmpty();
}
