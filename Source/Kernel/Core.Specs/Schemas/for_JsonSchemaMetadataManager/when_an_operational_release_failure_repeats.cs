// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager;

public class when_an_operational_release_failure_repeats : given.an_unrecoverable_value
{
    void Establish() => _error = new InvalidOperationException("storage failure");

    async Task Because()
    {
        for (var index = 0; index < 2; index++) await _manager.Release(EventStoreName.NotSet, EventStoreNamespaceName.Default, _schema, "subject", _input);
    }

    [Fact] void should_not_deduplicate_operational_failures() => _logger.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(ILogger.Log)).ShouldEqual(2);
    [Fact] void should_not_increment_the_unrecoverable_counter() => _count.ShouldEqual(0);
}
