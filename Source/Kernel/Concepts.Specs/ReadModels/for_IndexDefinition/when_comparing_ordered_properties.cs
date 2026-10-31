// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.ReadModels.for_IndexDefinition;

public class when_comparing_ordered_properties : Specification
{
    IndexDefinition _first;
    IndexDefinition _same;
    IndexDefinition _reversed;
    IndexDefinition _ascending;

    void Establish()
    {
        _first = new("name") { Properties = [new("name"), new("created", true)] };
        _same = new("name") { Properties = [new("name"), new("created", true)] };
        _reversed = new("created") { Properties = [new("created", true), new("name")] };
        _ascending = new("name") { Properties = [new("name"), new("created")] };
    }

    [Fact] void should_compare_independent_lists_by_value() => _first.ShouldEqual(_same);
    [Fact] void should_produce_the_same_hash_code() => _first.GetHashCode().ShouldEqual(_same.GetHashCode());
    [Fact] void should_not_reconcile_an_unchanged_definition() => new[] { _first }.SequenceEqual([_same]).ShouldBeTrue();
    [Fact] void should_distinguish_order() => _first.ShouldNotEqual(_reversed);
    [Fact] void should_distinguish_direction() => _first.ShouldNotEqual(_ascending);
    [Fact] void should_sign_order_and_direction() => new[] { _first.Signature, _reversed.Signature, _ascending.Signature }.Distinct().Count().ShouldEqual(3);
    [Fact] void should_recognize_composites() => _first.IsComposite.ShouldBeTrue();
    [Fact] void should_read_a_legacy_path_as_ascending() => new IndexDefinition("name").Properties.SequenceEqual([new IndexedProperty("name")]).ShouldBeTrue();
    [Fact] void should_compare_a_legacy_definition_with_an_explicit_ascending_definition() => new IndexDefinition("name").ShouldEqual(new IndexDefinition("name") { Properties = [new("name")] });
}
