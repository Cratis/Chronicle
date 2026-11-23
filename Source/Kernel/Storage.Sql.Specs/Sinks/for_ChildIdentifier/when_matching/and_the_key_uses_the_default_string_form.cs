// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_ChildIdentifier.when_matching;

public class and_the_key_uses_the_default_string_form : Specification
{
    bool _matching;
    bool _different;
    bool _typeName;

    void Because()
    {
        var identifier = ChildIdentifier.For("id", new given.composite_key("tenant", "target"));
        _matching = identifier.Matches(new given.composite_key("tenant", "target"), false);
        _different = identifier.Matches(new given.composite_key("tenant", "other"), false);
        _typeName = identifier.Matches(typeof(given.composite_key).ToString(), false);
    }

    [Fact] void should_match_a_fresh_key_with_the_same_properties() => _matching.ShouldBeTrue();
    [Fact] void should_not_match_different_properties() => _different.ShouldBeFalse();
    [Fact] void should_not_match_the_type_name() => _typeName.ShouldBeFalse();
}
