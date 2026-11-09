// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_an_event_content_dictionary_key_is_not_on_the_event : given.a_projection_factory_for_definitions
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(() => CreateFrom("slots.$eventContent.missing", "seriesId"));

    [Fact] void should_throw_event_content_key_not_in_event() => _result.ShouldBeOfExactType<EventContentKeyNotInEvent>();
}
