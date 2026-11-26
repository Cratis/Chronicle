// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_converting_to_update_definition;

public class and_an_object_child_without_an_identifier_is_removed : given.a_converter_rendering_updates
{
    void Establish() => _expandoObjectConverter.ToBsonDocument(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>()).Returns(new BsonDocument("Name", "full-document"));

    Task Because() => Convert(new Key("key", ArrayIndexers.NoIndexers), 42UL, new ChildRemoved(new ExpandoObject(), new PropertyPath("Children"), PropertyPath.Root, "child-key"));

    [Fact] void should_pull_the_whole_document() => _update["$pull"]["Children"].ShouldEqual(new BsonDocument("Name", "full-document"));
}
