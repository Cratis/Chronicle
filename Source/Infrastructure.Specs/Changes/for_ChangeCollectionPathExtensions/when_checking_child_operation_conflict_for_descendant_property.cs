// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_ChangeCollectionPathExtensions;

public class when_checking_child_operation_conflict_for_descendant_property : Specification
{
    PropertyDifference _childIdentifierDifference;
    PropertyDifference _childFieldDifference;
    PropertyDifference _siblingDifference;
    ISet<PropertyPath> _collectionPathsWithChildOperations;

    bool _childIdentifierResult;
    bool _childFieldResult;
    bool _siblingResult;

    void Establish()
    {
        _collectionPathsWithChildOperations = new HashSet<PropertyPath>
        {
            new("candidates")
        };

        _childIdentifierDifference = new PropertyDifference("[candidates].$eventSourceId", null, "candidate-1");
        _childFieldDifference = new PropertyDifference("[candidates].isCustomerSigned", false, true);
        _siblingDifference = new PropertyDifference("name", "Before", "After");
    }

    void Because()
    {
        _childIdentifierResult = _childIdentifierDifference.ConflictsWithChildOperation(_collectionPathsWithChildOperations);
        _childFieldResult = _childFieldDifference.ConflictsWithChildOperation(_collectionPathsWithChildOperations);
        _siblingResult = _siblingDifference.ConflictsWithChildOperation(_collectionPathsWithChildOperations);
    }

    [Fact] void should_treat_child_identifier_as_conflicting() => _childIdentifierResult.ShouldBeTrue();
    [Fact] void should_treat_child_field_as_conflicting() => _childFieldResult.ShouldBeTrue();
    [Fact] void should_not_treat_sibling_property_as_conflicting() => _siblingResult.ShouldBeFalse();
}
