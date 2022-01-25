// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Reactive.for_ObservableCollection
{
    public class when_adding_two_items_and_removing_one_of_two_items_and_subscribing_after : Specification
    {
        const string first_item = "First Item";
        const string second_item = "Second Item";

        ObservableCollection<string> collection;

        List<string> added;
        List<string> removed;
        List<IEnumerable<string>> changes;

        void Establish()
        {
            collection = new();
            added = new();
            removed = new();
            changes = new();
            collection.Subscribe(_ => changes.Add(_));
        }

        void Because()
        {
            collection.Add(first_item);
            collection.Add(second_item);
            collection.Remove(first_item);

            collection.Added.Subscribe(item => added.Add(item));
            collection.Removed.Subscribe(item => removed.Add(item));
        }

        [Fact] void should_have_both_items_added() => added.ShouldContainOnly(first_item, second_item);
        [Fact] void should_only_have_the_removed_items_removed() => removed.ShouldContainOnly(first_item);
        [Fact] void should_only_contain_the_non_removed_item() => collection.ShouldContainOnly(second_item);
        [Fact] void should_have_first_change_be_empty() => changes[0].ShouldBeEmpty();
        [Fact] void should_have_second_change_with_first_item() => changes[1].ShouldContainOnly(first_item);
        [Fact] void should_have_third_change_with_both_items() => changes[2].ShouldContainOnly(first_item, second_item);
        [Fact] void should_have_forth_change_with_first_item() => changes[3].ShouldContainOnly(second_item);
    }
}
