// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class it_passes_the_plaintext_content_to_migration_without_reserializing : given.an_event_sequence
{
    JsonObject _compliantContent;
    JsonObject _plaintextContent;
    JsonObject _contentPassedToMigration;

    void Establish()
    {
        _plaintextContent = new JsonObject { ["name"] = "Jane" };
        _compliantContent = new JsonObject { ["name"] = "protected Jane" };
        _complianceManager.Apply(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns(_ => Task.FromResult(_compliantContent));
        _eventTypeMigrations.MigrateToAllGenerations(Arg.Any<EventStoreName>(), Arg.Any<EventType>(), Arg.Any<JsonObject>(), Arg.Any<ExpandoObject>())
            .Returns(callInfo =>
            {
                _contentPassedToMigration = callInfo.ArgAt<JsonObject>(2);
                return new Dictionary<EventTypeGeneration, ExpandoObject>();
            });
    }

    Task Because() => _eventSequence.Append(
        EventSourceType.Default,
        _eventSourceId,
        EventStreamType.All,
        EventStreamId.Default,
        _eventType,
        _plaintextContent,
        CorrelationId.New(),
        [],
        Identity.System,
        [],
        ConcurrencyScope.None);

    [Fact] void should_pass_the_exact_plaintext_json_object_instance_to_migration() => _contentPassedToMigration.ShouldBeSame(_plaintextContent);
}
