// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering;

public class and_competing_clients_use_different_containers : given.a_read_models_manager
{
    static readonly ReadModelDefinition _original = DefinitionFor("model", "Model") with { ContainerName = "camelCase" };
    static readonly ReadModelDefinition _updated = _original with { ContainerName = "CamelCase" };
    Exception _beforeUpdate;
    Exception _afterUpdate;

    async Task Establish() => await _manager.RegisterSingle(_original);

    async Task Because()
    {
        _beforeUpdate = await Catch.Exception(() => _manager.RegisterSingle(_updated));
        await _manager.RegisterSingle(_original);
        await _manager.UpdateDefinition(_updated);
        _readModelGrain.ClearReceivedCalls();
        _afterUpdate = await Catch.Exception(() => _manager.RegisterSingle(_original));
        await _manager.RegisterSingle(_updated);
    }

    [Fact] void should_refuse_the_competing_client_before_explicit_update() => _beforeUpdate.ShouldBeOfExactType<ReadModelContainerChangeRequiresExplicitUpdate>();
    [Fact] void should_refuse_the_old_client_after_explicit_update() => _afterUpdate.ShouldBeOfExactType<ReadModelContainerChangeRequiresExplicitUpdate>();
    [Fact] async Task should_not_silently_restore_the_old_container() => (await _manager.GetDefinitions()).ShouldContainOnly(_updated);
    [Fact] void should_write_only_the_first_registration_and_explicit_update() => _silo.StorageManager.GetStorageStats(typeof(ReadModelsManager).FullName)!.Writes.ShouldEqual(2);
    [Fact] void should_not_invalidate_the_updated_clients_completed_cache() => _readModelGrain.ReceivedCalls().ShouldBeEmpty();
}
