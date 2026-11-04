// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Concepts.Keys.for_Key;

public class when_converting_from_an_event_source_id : Specification
{
    Key _key;

    void Because() => _key = new EventSourceId("abc");

    [Fact] void should_keep_the_string_value() => _key.Value.ShouldEqual("abc");
    [Fact] void should_have_string_kind() => _key.Kind.ShouldEqual(KeyKind.String);
    [Fact] void should_have_no_indexers() => _key.ArrayIndexers.ShouldEqual(ArrayIndexers.NoIndexers);
}
