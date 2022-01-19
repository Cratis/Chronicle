// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Reactive.for_ObservableCollection
{
    public class when_clearing : Specification
    {
        ObservableCollection<string> collection;
        bool clear_called;

        void Establish()
        {
            collection = new();
            collection.Add("something");
            clear_called = false;
            collection.Cleared += () => clear_called = true;
        }

        void Because() => collection.Clear();

        [Fact] void should_trigger_cleared_event() => clear_called.ShouldBeTrue();
        [Fact] void should_clear_the_collection() => collection.ShouldBeEmpty();
    }
}
