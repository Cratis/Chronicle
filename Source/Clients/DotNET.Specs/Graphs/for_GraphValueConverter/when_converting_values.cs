// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs.for_GraphValueConverter;

public class when_converting_values : Specification
{
    static readonly Guid _id = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public enum Status
    {
        Active = 0,
        Closed = 1
    }

    public record Identifier(Guid Value) : ConceptAs<Guid>(Value);

    object? _concept;
    object? _guid;
    object? _enum;
    object? _decimal;
    object? _integer;
    object? _list;

    void Because()
    {
        _concept = GraphValueConverter.ToGraph(new Identifier(_id));
        _guid = GraphValueConverter.ToGraph(_id);
        _enum = GraphValueConverter.ToGraph(Status.Closed);
        _decimal = GraphValueConverter.ToGraph(2.5m);
        _integer = GraphValueConverter.ToGraph(42);
        _list = GraphValueConverter.ToGraph(new[] { 1, 2 });
    }

    [Fact] void should_convert_a_concept_as_the_value_it_wraps() => _concept.ShouldEqual(_id.ToString());
    [Fact] void should_convert_a_guid_to_its_string() => _guid.ShouldEqual(_id.ToString());
    [Fact] void should_convert_an_enum_to_its_name() => _enum.ShouldEqual("Closed");
    [Fact] void should_convert_a_decimal_to_a_double() => _decimal.ShouldEqual(2.5d);
    [Fact] void should_convert_a_whole_number_to_a_long() => _integer.ShouldEqual(42L);
    [Fact] void should_convert_a_list_item_by_item() => ((IEnumerable<object?>)_list!).ShouldEqual([1L, 2L]);
}
