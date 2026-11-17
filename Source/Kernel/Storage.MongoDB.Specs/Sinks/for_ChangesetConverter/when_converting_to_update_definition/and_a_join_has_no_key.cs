// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_converting_to_update_definition;

public class and_a_join_has_no_key : given.a_converter_rendering_updates
{
    Task Because() => Convert(
        new Key("key", ArrayIndexers.NoIndexers),
        42UL,
        new Joined(new ExpandoObject(), null!, new PropertyPath("joinKey"), ArrayIndexers.NoIndexers, [new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("name", "Before", "After")])]));

    [Fact] void should_not_update_any_joined_documents() => _joinUpdates.ShouldBeEmpty();
}
