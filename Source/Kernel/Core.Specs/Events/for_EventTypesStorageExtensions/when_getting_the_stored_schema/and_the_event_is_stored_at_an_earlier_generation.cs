// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.for_EventTypesStorageExtensions.when_getting_the_stored_schema;

public class and_the_event_is_stored_at_an_earlier_generation : given.an_event_type_stored_at_two_generations
{
    JsonSchema? _schema;

    async Task Because() => _schema = await _eventTypes.GetStoredSchemaFor(first_generation);

    [Fact] void should_return_the_schema_of_that_generation() => _schema.ShouldEqual(_first_generation_schema.Schema);
}
