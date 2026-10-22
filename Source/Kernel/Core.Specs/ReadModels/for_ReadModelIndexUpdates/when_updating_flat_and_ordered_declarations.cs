// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Services.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelIndexUpdates;

public class when_updating_flat_and_ordered_declarations : Specification
{
    IndexDefinition _compound;
    IndexDefinition _descending;
    IndexDefinition[] _existing;

    void Establish()
    {
        _compound = new("name") { Properties = [new("name"), new("created", true)] };
        _descending = new("total") { Properties = [new("total", true)] };
        _existing = [_compound, _descending, new("old")];
    }

    [Fact] void should_not_flatten_composites_from_a_legacy_editor() => ReadModelIndexUpdates.Apply([new() { PropertyPath = "name" }, new() { PropertyPath = "new" }], _existing).SequenceEqual([new IndexDefinition("new"), _compound, _descending]).ShouldBeTrue();
    [Fact] void should_not_erase_ordered_definitions_from_an_empty_legacy_update() => ReadModelIndexUpdates.Apply([], _existing).SequenceEqual([_compound, _descending]).ShouldBeTrue();
    [Fact] void should_allow_an_ordered_contract_to_replace_the_definitions() => ReadModelIndexUpdates.Apply([new() { PropertyPath = "replacement", Properties = [new() { Path = "replacement", Descending = true }] }], _existing).SequenceEqual([new IndexDefinition("replacement") { Properties = [new("replacement", true)] }]).ShouldBeTrue();
}
