// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Reactive.for_ObservableCollection
{
    public class when_clearing : Specification
    {
        ObservableCollection<string> collection;
        bool clear_called;
        List<IEnumerable<string>> changes;

        void Establish()
        {
            changes = new();
            collection = new();
            collection.Subscribe(_ => changes.Add(_));
            collection.Add("something");
            clear_called = false;
            collection.Cleared += () => clear_called = true;
        }

        void Because() => collection.Clear();

        [Fact] void should_trigger_cleared_event() => clear_called.ShouldBeTrue();
        [Fact] void should_clear_the_collection() => collection.ShouldBeEmpty();
        [Fact] void should_have_first_change_be_empty() => changes[0].ShouldBeEmpty();
        [Fact] void should_have_second_change_with_item() => changes[1].ShouldContainOnly("something");
        [Fact] void should_have_third_change_as_empty() => changes[2].ShouldBeEmpty();
    }
}
