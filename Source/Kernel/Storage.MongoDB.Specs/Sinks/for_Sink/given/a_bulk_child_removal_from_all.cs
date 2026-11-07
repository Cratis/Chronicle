// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.given;

public class a_bulk_child_removal_from_all : a_child_removal_from_all
{
    async Task Establish()
    {
        _converter.ToBsonValue(Arg.Any<Key>()).Returns(call => new BsonString(call.Arg<Key>().Value.ToString()));
        _changesetConverter.ToUpdateDefinition(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>())
            .Returns(new UpdateDefinitionAndArrayFilters(new BsonDocument("$set", new BsonDocument("pending", true)), [], true));
        await _sink.BeginBulk();
    }

    protected async Task Cache(Key key, ExpandoObject state)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.CurrentState.Returns(state);
        changeset.Changes.Returns([]);
        await _sink.ApplyChanges(key, changeset, EventSequenceNumber.First);
    }

    protected static ExpandoObject Document(string property, object? value)
    {
        var document = new ExpandoObject();
        ((IDictionary<string, object?>)document)[property] = value;
        return document;
    }
}
