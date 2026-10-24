// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Generations.Bindings.for_GenerationCatalog;

public class when_setting_the_binding_epoch : given.a_catalog
{
    bool _result;
    void Establish() => _storage.TrySetBindingEpoch(_store, _key, ServingEpoch.Unset, ServingEpoch.Initial).Returns(true);
    async Task Because() => _result = await _catalog.TrySetBindingEpoch(_store, _namespace, _key.Projection, 0, 1);
    [Fact] void should_pass_the_typed_epochs_and_return_the_result() => _result.ShouldBeTrue();
    [Fact] async Task should_pass_the_resolved_key() => await _storage.Received(1).TrySetBindingEpoch(_store, _key, ServingEpoch.Unset, ServingEpoch.Initial);
}
