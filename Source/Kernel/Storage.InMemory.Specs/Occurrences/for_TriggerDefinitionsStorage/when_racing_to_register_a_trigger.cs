// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.InMemory.Occurrences.for_TriggerDefinitionsStorage;

public class when_racing_to_register_a_trigger : Specification
{
    readonly TriggerDefinitionsStorage _storage = new();
    readonly TriggerDefinition _definition = new(new TriggerId("payment-import"), "Importer", "{}", 1);
    bool[] _saved;
    TriggerDefinition[] _stored;

    async Task Because()
    {
        _saved = await Task.WhenAll(Task.Run(() => _storage.TrySave(_definition, null)), Task.Run(() => _storage.TrySave(_definition with { Name = "Other" }, null)));
        _stored = (await _storage.GetAll()).ToArray();
    }

    [Fact] void should_allow_exactly_one_writer() => _saved.Count(saved => saved).ShouldEqual(1);
    [Fact] void should_store_exactly_one_definition() => _stored.Length.ShouldEqual(1);
    [Fact] void should_preserve_the_original_identity() => _stored[0].Id.ShouldEqual(_definition.Id);
}
