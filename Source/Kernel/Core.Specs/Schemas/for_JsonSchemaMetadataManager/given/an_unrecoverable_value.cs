// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.ProtectedValues;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.given;

public class an_unrecoverable_value : a_value_handler_and_a_type_with_one_property
{
    protected ILogger<JsonSchemaMetadataManager> _logger;
    protected int _count;
    protected Exception _error;

    void Establish()
    {
        _error = new EncryptionKeyUnrecoverable(3, new InvalidOperationException("sensitive details"));
        _logger = Substitute.For<ILogger<JsonSchemaMetadataManager>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        var reporter = new UnrecoverableProtectedValueReporter(_logger, TimeProvider.System, () => _count++);
        _manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(_valueHandler), _logger, reporter);
        _valueHandler.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<JsonNode>())
            .Returns(_ => Task.FromException<JsonNode>(_error));
        _valueHandler.ReleaseStrict(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<JsonNode>())
            .Returns(_ => Task.FromException<JsonNode>(_error));
        _valueHandler.ReleaseWithStatus(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<JsonNode>())
            .Returns(_ => Task.FromException<ReleasedProtectedValue>(_error));
    }
}
