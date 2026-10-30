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
    [Fact] void should_keep_a_standalone_index_alongside_a_composite_lead() => ReadModelIndexUpdates.Apply([new() { PropertyPath = "name" }, new() { PropertyPath = "name" }], [new("name"), _compound]).SequenceEqual([new IndexDefinition("name"), _compound]).ShouldBeTrue();
    [Fact] void should_keep_both_directions_from_an_unchanged_legacy_update() => ReadModelIndexUpdates.Apply([new() { PropertyPath = "total" }, new() { PropertyPath = "total" }], [new("total"), _descending]).SequenceEqual([new IndexDefinition("total"), _descending]).ShouldBeTrue();
    [Fact] void should_keep_an_existing_standalone_even_with_one_ambiguous_path() => ReadModelIndexUpdates.Apply([new() { PropertyPath = "name" }], [new("name"), _compound]).SequenceEqual([new IndexDefinition("name"), _compound]).ShouldBeTrue();
    [Fact] void should_keep_a_surplus_flat_path_alongside_a_composite_lead() => ReadModelIndexUpdates.Apply([new() { PropertyPath = "name" }, new() { PropertyPath = "name" }], [_compound]).SequenceEqual([new IndexDefinition("name"), _compound]).ShouldBeTrue();
    [Fact] void should_keep_a_surplus_flat_path_alongside_a_descending_index() => ReadModelIndexUpdates.Apply([new() { PropertyPath = "total" }, new() { PropertyPath = "total" }], [_descending]).SequenceEqual([new IndexDefinition("total"), _descending]).ShouldBeTrue();
    [Fact]
    void should_count_every_retained_ordered_lead_before_keeping_surplus_paths()
    {
        var descending = new IndexDefinition("name") { Properties = [new("name", true)] };
        ReadModelIndexUpdates.Apply([new() { PropertyPath = "name" }, new() { PropertyPath = "name" }, new() { PropertyPath = "name" }], [_compound, descending]).SequenceEqual([new IndexDefinition("name"), _compound, descending]).ShouldBeTrue();
    }

    [Fact]
    void should_not_turn_multiple_ordered_echoes_into_a_standalone()
    {
        var descending = new IndexDefinition("name") { Properties = [new("name", true)] };
        ReadModelIndexUpdates.Apply([new() { PropertyPath = "name" }, new() { PropertyPath = "name" }], [_compound, descending]).SequenceEqual([_compound, descending]).ShouldBeTrue();
    }

    [Fact] void should_not_turn_a_descending_echo_into_a_standalone() => ReadModelIndexUpdates.Apply([new() { PropertyPath = "total" }], [_descending]).SequenceEqual([_descending]).ShouldBeTrue();
    [Fact] void should_allow_an_explicit_empty_replacement() => ReadModelIndexUpdates.Apply([], _existing, replace: true).ShouldBeEmpty();
    [Fact] void should_not_keep_old_composites_in_an_explicit_flat_replacement() => ReadModelIndexUpdates.Apply([new() { PropertyPath = "new" }], _existing, replace: true).SequenceEqual([new IndexDefinition("new")]).ShouldBeTrue();
    [Fact] void should_allow_an_ordered_contract_to_replace_the_definitions() => ReadModelIndexUpdates.Apply([new() { PropertyPath = "replacement", Properties = [new() { Path = "replacement", Descending = true }] }], _existing).SequenceEqual([new IndexDefinition("replacement") { Properties = [new("replacement", true)] }]).ShouldBeTrue();
}
