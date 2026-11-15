// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Concepts.Keys.for_Key;

public class when_converting_to_an_event_source_id : Specification
{
    [Fact] void should_convert_a_string_key() => ((EventSourceId)new Key("abc", ArrayIndexers.NoIndexers)).ShouldEqual(new EventSourceId("abc"));
    [Fact] void should_convert_a_guid_key() => ((EventSourceId)new Key(Guid.Empty, ArrayIndexers.NoIndexers)).ShouldEqual(new EventSourceId(Guid.Empty.ToString()));
    [Fact] void should_convert_an_undefined_key() => ((EventSourceId)Key.Undefined).ShouldEqual(EventSourceId.Unspecified);
}
