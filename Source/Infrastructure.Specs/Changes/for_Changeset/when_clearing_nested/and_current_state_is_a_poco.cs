// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_clearing_nested;

public class and_current_state_is_a_poco : Specification
{
    sealed class NestedState
    {
        public string Name { get; set; } = "Before";
    }

    sealed class ReadModel
    {
        public NestedState? Info { get; set; } = new();
    }

    Changeset<object, ReadModel> _changeset;

    void Establish() => _changeset = new Changeset<object, ReadModel>(Substitute.For<IObjectComparer>(), new object(), new ReadModel());

    void Because() => _changeset.ClearNested("Info", ArrayIndexers.NoIndexers);

    [Fact] void should_clear_the_current_state() => _changeset.CurrentState.Info.ShouldBeNull();
    [Fact] void should_leave_the_initial_state_unchanged() => _changeset.InitialState.Info.ShouldNotBeNull();
}
