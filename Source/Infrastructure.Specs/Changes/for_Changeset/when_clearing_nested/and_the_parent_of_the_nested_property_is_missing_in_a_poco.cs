// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_clearing_nested;

public class and_the_parent_of_the_nested_property_is_missing_in_a_poco : Specification
{
    sealed class NestedState
    {
        public string Name { get; set; } = "Before";
    }

    sealed class OuterState
    {
        public NestedState? Info { get; set; } = new();
    }

    sealed class ReadModel
    {
        public OuterState? Outer { get; set; }
    }

    Changeset<object, ReadModel> _changeset;

    void Establish() => _changeset = new Changeset<object, ReadModel>(Substitute.For<IObjectComparer>(), new object(), new ReadModel());

    void Because() => _changeset.ClearNested("Outer.Info", ArrayIndexers.NoIndexers);

    [Fact] void should_keep_the_parent_missing() => _changeset.CurrentState.Outer.ShouldBeNull();
    [Fact] void should_still_record_the_clear() => _changeset.Changes.Single().ShouldBeOfExactType<NestedCleared>();
}
