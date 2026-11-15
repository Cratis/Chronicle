// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_an_event_content_dictionary_key_is_on_the_event : given.a_projection_factory_for_definitions
{
    IProjection _result;

    async Task Because() => _result = await CreateFrom("slots.$eventContent.slot", "seriesId");

    [Fact] void should_create_the_projection() => _result.ShouldNotBeNull();
}
