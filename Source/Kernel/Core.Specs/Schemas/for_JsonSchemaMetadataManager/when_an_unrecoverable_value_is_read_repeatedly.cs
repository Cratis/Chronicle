// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager;

public class when_an_unrecoverable_value_is_read_repeatedly : given.an_unrecoverable_value
{
    JsonObject _result;

    async Task Because()
    {
        for (var index = 0; index < 3; index++) _result = await _manager.Release(EventStoreName.NotSet, EventStoreNamespaceName.Default, _schema, "subject", _input);
    }

    [Fact] void should_keep_returning_an_empty_property() => _result[PropertyName]!.GetValue<int>().ShouldEqual(0);
    [Fact] void should_count_once() => _count.ShouldEqual(1);
    [Fact] void should_log_once_without_generic_duplicates() => _logger.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(ILogger.Log)).ShouldEqual(1);
}
