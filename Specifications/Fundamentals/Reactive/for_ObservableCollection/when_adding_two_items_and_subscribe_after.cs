// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Reactive.for_ObservableCollection
{
    public class when_adding_two_items_and_subscribe_after : Specification
    {
        const string first_item = "First Item";
        const string second_item = "Second Item";

        ObservableCollection<string> collection;

        List<IEnumerable<string>> changes;

        void Establish()
        {
            collection = new();
            changes = new();
        }

        void Because()
        {
            collection.Add(first_item);
            collection.Add(second_item);
            collection.Subscribe(_ => changes.Add(_));
        }

        [Fact] void should_be_called_both_items() => changes[0].ShouldContainOnly(first_item, second_item);
    }
}
