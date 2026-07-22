// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Storage.Projections.for_CachingProjectionDefinitionsStorage;

public class when_getting_a_definition_after_saving : given.a_caching_projection_definitions_storage
{
    ProjectionDefinition _result;

    Task Establish() => _storage.Save(_definition);

    async Task Because() => _result = await _storage.Get(_id);

    [Fact] void should_save_to_inner() => _inner.Received(1).Save(_definition);
    [Fact] void should_not_ask_inner_for_the_definition() => _inner.DidNotReceive().Get(_id);
    [Fact] void should_return_the_saved_definition() => _result.ShouldEqual(_definition);
}
