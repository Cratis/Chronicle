// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_looking_up_by_composite_key.given;

public class a_composite_key_lookup : for_Sink.given.a_sink_observing_a_container
{
    protected CompositeKeyLookupResult _result;
    protected Key _key = new(new ExpandoObject(), ArrayIndexers.NoIndexers);
    protected BsonDocument _id = new() { { "a", "a" }, { "b", "b" } };
    void Establish()
    {
        _primaryId = Guid.NewGuid();
        _converter.ToBsonValue(_key).Returns(_ => _id);
    }
    protected Task<CompositeKeyLookupResult> Lookup() => _sink.FindByCompositeKey(_key);
}
