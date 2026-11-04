// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Diagnostics.for_StorageAuthorizationDiagnostics.when_clearing;

public class and_another_collection_failed : given.a_registry
{
    void Establish() => _registry.Record(_failure);
    void Because() => _registry.Clear("System", "OtherCollection");

    [Fact] void should_keep_the_failure() => _registry.Failures.ShouldContainOnly(_failure);
    [Fact] void should_not_log_a_recovery() => _logger.Messages(LogLevel.Information).ShouldBeEmpty();
}
