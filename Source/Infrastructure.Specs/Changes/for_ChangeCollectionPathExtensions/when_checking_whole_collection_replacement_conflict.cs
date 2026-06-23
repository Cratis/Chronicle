// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_ChangeCollectionPathExtensions;

public class when_checking_whole_collection_replacement_conflict : Specification
{
    PropertyDifference _wholeCollection;
    PropertyDifference _childField;
    PropertyDifference _sibling;
    ISet<PropertyPath> _wholeCollectionReplacementPaths;

    bool _wholeCollectionResult;
    bool _childFieldResult;
    bool _siblingResult;

    void Establish()
    {
        _wholeCollectionReplacementPaths = new HashSet<PropertyPath> { new("contacts") };

        _wholeCollection = new PropertyDifference("[contacts]", null, "replacement");
        _childField = new PropertyDifference("[contacts].label", "old", "new");
        _sibling = new PropertyDifference("status", "pending", "approved");
    }

    void Because()
    {
        _wholeCollectionResult = _wholeCollection.ConflictsWithWholeCollectionReplacement(_wholeCollectionReplacementPaths);
        _childFieldResult = _childField.ConflictsWithWholeCollectionReplacement(_wholeCollectionReplacementPaths);
        _siblingResult = _sibling.ConflictsWithWholeCollectionReplacement(_wholeCollectionReplacementPaths);
    }

    [Fact] void should_not_treat_the_whole_collection_replacement_itself_as_conflicting() => _wholeCollectionResult.ShouldBeFalse();

    [Fact] void should_treat_a_child_field_under_the_collection_as_conflicting() => _childFieldResult.ShouldBeTrue();

    [Fact] void should_not_treat_a_sibling_property_as_conflicting() => _siblingResult.ShouldBeFalse();
}
