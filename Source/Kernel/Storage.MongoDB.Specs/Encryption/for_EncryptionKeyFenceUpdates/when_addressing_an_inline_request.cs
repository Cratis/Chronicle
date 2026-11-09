// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyFenceUpdates;

public class when_addressing_an_inline_request : Specification
{
    BsonDocument _has;
    BsonDocument _hasNot;
    BsonDocument _update;

    void Because()
    {
        var registry = BsonSerializer.SerializerRegistry;
        var fences = new RenderArgs<EncryptionKeyErasureForIdentifier>(registry.GetSerializer<EncryptionKeyErasureForIdentifier>(), registry);
        _has = EncryptionKeyFenceUpdates.HasRequest("request").Render(fences);
        _hasNot = (!EncryptionKeyFenceUpdates.HasRequest("request")).Render(fences);
        _update = EncryptionKeyFenceUpdates.UpdateRequest(new EncryptionKeyErasure(7u, [], false, 3), new("request", 7u, 2, Confirmed: true)).Render(fences).AsBsonDocument;
    }

    [Fact] void should_match_the_request_inside_the_log() => _has["requests"]["$elemMatch"]["requestId"].AsString.ShouldEqual("request");
    [Fact] void should_exclude_a_logged_request() => _hasNot["requests"]["$not"]["$elemMatch"]["requestId"].AsString.ShouldEqual("request");
    [Fact] void should_replace_the_matched_request() => _update["$set"]["requests.$"]["confirmed"].AsBoolean.ShouldBeTrue();
}
