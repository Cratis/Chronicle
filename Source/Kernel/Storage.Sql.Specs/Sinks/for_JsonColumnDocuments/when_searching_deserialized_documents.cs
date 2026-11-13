// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_JsonColumnDocuments;

public class when_searching_deserialized_documents : Specification
{
    object? _document;
    IPropertyPathSegment[] _segments;
    bool _foundInList;
    bool _foundAsString;
    bool _missing;
    bool _nullNode;
    bool _looseMatch;
    bool _conceptMatch;
    bool _bothNull;
    bool _oneNull;
    string _subPath;
    string _emptySubPath;

    void Establish()
    {
        _document = JsonColumnDocuments.DeserializeJsonColumn("""[{"details":{"code":"a1","number":7}},{"details":{"code":"b2"}}]""");
        _segments = new PropertyPath("column.details.code").Segments.ToArray();
    }

    void Because()
    {
        _foundInList = JsonColumnSearch.TryFindValueInDeserialized(_document, _segments, 1, "b2");
        _foundAsString = JsonColumnSearch.TryFindValueInDeserialized(_document, new PropertyPath("column.details.number").Segments.ToArray(), 1, 7);
        _missing = JsonColumnSearch.TryFindValueInDeserialized(_document, _segments, 1, "c3");
        _nullNode = JsonColumnSearch.TryFindValueInDeserialized(null, _segments, 1, "a1");
        _looseMatch = JsonColumnSearch.CompareLoose(5L, 5);
        _conceptMatch = JsonColumnSearch.CompareLoose(new given.a_value("x"), "x");
        _bothNull = JsonColumnSearch.CompareLoose(null, null);
        _oneNull = JsonColumnSearch.CompareLoose("x", null);
        _subPath = JsonColumnDocuments.BuildSubPath(new PropertyPath("column.details.code")).Path;
        _emptySubPath = JsonColumnDocuments.BuildSubPath(new PropertyPath("column")).Path;
    }

    [Fact] void should_find_a_value_in_any_list_element() => _foundInList.ShouldBeTrue();
    [Fact] void should_find_a_value_by_its_string_form() => _foundAsString.ShouldBeTrue();
    [Fact] void should_not_find_a_missing_value() => _missing.ShouldBeFalse();
    [Fact] void should_not_find_in_a_null_document() => _nullNode.ShouldBeFalse();
    [Fact] void should_match_loosely_by_string_form() => _looseMatch.ShouldBeTrue();
    [Fact] void should_match_a_concept_by_its_value() => _conceptMatch.ShouldBeTrue();
    [Fact] void should_match_two_nulls() => _bothNull.ShouldBeTrue();
    [Fact] void should_not_match_a_value_and_null() => _oneNull.ShouldBeFalse();
    [Fact] void should_drop_the_column_from_the_sub_path() => _subPath.ShouldEqual("details.code");
    [Fact] void should_have_an_empty_sub_path_for_the_column_itself() => _emptySubPath.ShouldEqual(string.Empty);
}
