// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_applying_changes;

[Collection(MongoDBCollection.Name)]
public class with_setting_array_to_empty_then_pushing_child(ChronicleInProcessFixture fixture) : given.a_sink(fixture)
{
    Key _key;
    Exception? _exception;
    BsonDocument? _resultDocument;

    void Establish()
    {
        _key = new Key("test-key-1", ArrayIndexers.NoIndexers);
    }

    async Task Because()
    {
        try
        {
            var comparer = new ObjectComparer();
            var incomingEvent = AppendedEvent.EmptyWithEventType(new("TestEvent", 1));
            var initialState = new ExpandoObject();
            var changeset = new Changeset<AppendedEvent, ExpandoObject>(comparer, incomingEvent, initialState);

            // First change: Set the items array to empty
            var propertiesChanged = new PropertiesChanged<ExpandoObject>(
                new ExpandoObject(),
                [
                    new PropertyDifference(
                        new PropertyPath("items"),
                        null,
                        Array.Empty<object>(),
                        ArrayIndexers.NoIndexers)
                ]);
            changeset.Add(propertiesChanged);

            // Second change: Add a primitive string child to the items array (to avoid schema complexity)
            var childAdded = new ChildAdded(
                "test-value",  // Using a primitive string instead of ExpandoObject
                new PropertyPath("items"),
                new PropertyPath("itemId"),
                "child-1",
                ArrayIndexers.NoIndexers);
            changeset.Add(childAdded);

            await _sink.ApplyChanges(_key, changeset, EventSequenceNumber.First);

            _resultDocument = GetDocument("test-key-1");
        }
        catch (Exception ex)
        {
            _exception = ex;
        }
    }

    [Fact] void should_not_throw_exception() => _exception.ShouldBeNull();
    [Fact] void should_have_created_document() => _resultDocument.ShouldNotBeNull();
    [Fact]
    void should_have_one_item_in_items_array()
    {
        _resultDocument.ShouldNotBeNull();
        var items = _resultDocument["items"].AsBsonArray;
        items.Count.ShouldEqual(1);
    }

    [Fact]
    void should_have_correct_value_in_items_array()
    {
        _resultDocument.ShouldNotBeNull();
        var items = _resultDocument["items"].AsBsonArray;
        items[0].AsString.ShouldEqual("test-value");
    }
}
