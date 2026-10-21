// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateStorage.when_saving;

public class with_a_webhook_retirement : Specification
{
    ObserverStateStorage _storage;
    KernelObserverState _state;
    BsonDocument _set;
    BsonDocument _expected;

    void Establish()
    {
        var database = Substitute.For<IEventStoreNamespaceDatabase>();
        var collection = Substitute.For<IMongoCollection<ObserverState>>();
        database.GetObserverStateCollection().Returns(collection);
        collection.UpdateOneAsync(
            Arg.Any<FilterDefinition<ObserverState>>(),
            Arg.Do<UpdateDefinition<ObserverState>>(update =>
            {
                var registry = BsonSerializer.SerializerRegistry;
                _set = update.Render(new RenderArgs<ObserverState>(registry.GetSerializer<ObserverState>(), registry))["$set"].AsBsonDocument;
            }),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>()).Returns(new UpdateResult.Acknowledged(1, 1, null));
        _storage = new(database);
        _state = new()
        {
            Identifier = "observer",
            WebhookRetirement = new(Guid.NewGuid(), DateTimeOffset.UtcNow, "https://receiver.example"),
            WebhookRegistrationId = Guid.NewGuid(),
            WebhookRegistrationFingerprint = "opaque-definition",
            WebhookReplayRetained = true
        };
        _expected = _state.ToMongoDB().ToBsonDocument();
    }

    async Task Because() => await _storage.Save(_state);

    [Fact] void should_set_the_webhook_retirement() => ValueIn(_set, nameof(ObserverState.WebhookRetirement)).ShouldEqual(ValueIn(_expected, nameof(ObserverState.WebhookRetirement)));

    [Fact] void should_set_the_retained_replay_marker() => ValueIn(_set, nameof(ObserverState.WebhookReplayRetained)).ShouldEqual(BsonBoolean.True);

    [Fact] void should_set_the_registration_identity() => ValueIn(_set, nameof(ObserverState.WebhookRegistrationId)).ShouldEqual(ValueIn(_expected, nameof(ObserverState.WebhookRegistrationId)));
    [Fact] void should_set_the_registration_fingerprint() => ValueIn(_set, nameof(ObserverState.WebhookRegistrationFingerprint)).ShouldEqual(ValueIn(_expected, nameof(ObserverState.WebhookRegistrationFingerprint)));

    static BsonValue ValueIn(BsonDocument document, string property) => document[BsonClassMap.LookupClassMap(typeof(ObserverState)).GetMemberMap(property).ElementName];
}
