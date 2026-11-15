// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Keys.for_Key;

public class when_serializing_to_bson : Specification
{
    BsonDocument _document;

    void Because() => _document = new Key("partition", ArrayIndexers.NoIndexers).ToBsonDocument();

    [Fact] void should_not_persist_camel_case_kind() => _document.Contains("kind").ShouldBeFalse();
    [Fact] void should_not_persist_pascal_case_kind() => _document.Contains("Kind").ShouldBeFalse();
}
