// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Concepts.Specs.Events.Constraints.for_ResolvedConstraintScopeExtensions.when_bridging_to_the_legacy_scope_key;

/// <summary>
/// The bridge reproduces the legacy key faithfully, which means it also reproduces what was wrong with it. Two
/// genuinely different dimension tuples flatten to the same text when a value carries the characters the key joins
/// on, so the flattening is deterministic but not injective and a scope can never be recovered from a key. Stating
/// that here keeps the claim honest: routing a provider that only implements the string-keyed member through the
/// bridge leaves it exactly as correct as it already was, and repairs nothing.
/// </summary>
public class and_two_different_scopes_flatten_to_the_same_text : Specification
{
    static readonly ResolvedConstraintScope _oneScope = new((EventSourceType)"loan|estt:branch", (EventStreamType)"region");
    static readonly ResolvedConstraintScope _anotherScope = new((EventSourceType)"loan", (EventStreamType)"branch|estt:region");

    string _oneKey;
    string _anotherKey;

    void Because()
    {
        _oneKey = _oneScope.ToLegacyScopeKey();
        _anotherKey = _anotherScope.ToLegacyScopeKey();
    }

    [Fact] void should_keep_the_typed_scopes_distinct() => _oneScope.ShouldNotEqual(_anotherScope);
    [Fact] void should_flatten_both_to_the_same_key() => _oneKey.ShouldEqual(_anotherKey);
}
