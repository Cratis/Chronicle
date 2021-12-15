// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Reactive.for_ObservableCollection
{
    public class when_removing_one_of_two_items_and_subscribing_after : Specification
    {
        const string first_item = "First Item";
        const string second_item = "Second Item";

        ObservableCollection<string> collection;

        List<string> added;
        List<string> removed;

        void Establish()
        {
            collection = new();
            added = new();
            removed = new();
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
    }
}
