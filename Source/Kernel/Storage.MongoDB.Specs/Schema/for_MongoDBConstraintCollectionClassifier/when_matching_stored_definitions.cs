// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBConstraintCollectionClassifier;

public class when_matching_stored_definitions : Specification
{
    UniqueConstraintDefinition[] _definitions;
    UniqueConstraintMode? _namedValues;
    UniqueConstraintMode? _scopedSource;
    UniqueConstraintMode? _perValue;
    UniqueConstraintMode? _scopedPerValue;
    UniqueConstraintMode? _unknown;
    UniqueConstraintMode? _ambiguous;
    UniqueConstraintMode? _inapplicable;
    UniqueConstraintMode? _agreeingCandidates;

    void Establish() => _definitions =
    [
        new("values", []),
        new("retained", []) { Mode = UniqueConstraintMode.PerValue }
    ];

    void Because()
    {
        _namedValues = MongoDBConstraintCollectionClassifier.Classify("event-log+values+constraint", _definitions, ["event-log"]);
        _scopedSource = MongoDBConstraintCollectionClassifier.Classify("event-log+values+scope+constraint", _definitions, ["event-log"]);
        _perValue = MongoDBConstraintCollectionClassifier.Classify("event-log+retained+values+constraint", _definitions, ["event-log"]);
        _scopedPerValue = MongoDBConstraintCollectionClassifier.Classify("event-log+retained+scope+values+constraint", _definitions, ["event-log"]);
        _unknown = MongoDBConstraintCollectionClassifier.Classify("event-log+unknown+constraint", _definitions, ["event-log"]);
        _ambiguous = MongoDBConstraintCollectionClassifier.Classify(
            "event-log+email+values+constraint",
            [new("email", []) { Mode = UniqueConstraintMode.PerValue }, new("email+values", [])],
            ["event-log"]);
        _agreeingCandidates = MongoDBConstraintCollectionClassifier.Classify(
            "event-log+email+scope+constraint",
            [new("email", []), new("email+scope", [])],
            ["event-log"]);
        _inapplicable = MongoDBConstraintCollectionClassifier.Classify(
            "event-log+values+constraint",
            [new("values", []) { EventSequences = ["outbox"] }],
            ["event-log"]);
    }

    [Fact] void should_use_the_definition_for_a_per_source_constraint_named_values() => _namedValues.ShouldEqual(UniqueConstraintMode.PerEventSource);
    [Fact] void should_match_a_scoped_per_source_collection() => _scopedSource.ShouldEqual(UniqueConstraintMode.PerEventSource);
    [Fact] void should_match_a_per_value_collection() => _perValue.ShouldEqual(UniqueConstraintMode.PerValue);
    [Fact] void should_match_a_scoped_per_value_collection() => _scopedPerValue.ShouldEqual(UniqueConstraintMode.PerValue);
    [Fact] void should_leave_unknown_collections_unclassified() => _unknown.ShouldBeNull();
    [Fact] void should_leave_conflicting_modes_unclassified() => _ambiguous.ShouldBeNull();
    [Fact] void should_accept_multiple_candidates_when_their_modes_agree() => _agreeingCandidates.ShouldEqual(UniqueConstraintMode.PerEventSource);
    [Fact] void should_not_use_a_definition_for_another_sequence() => _inapplicable.ShouldBeNull();
}
