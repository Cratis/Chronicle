// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionEventContextExtensions.when_joining;

public class and_inner_subscription_is_disposed : given.a_projection_event_context_observable
{
    bool _itemReceived;
    IDisposable _subscription;

    void Establish()
    {
        _subscription = _subject
            .Join(PropertyPath.Root)
            .Subscribe(_ => _itemReceived = true);
    }

    void Because()
    {
        _subscription.Dispose();
        _subject.OnNext(_eventContext);
    }

    [Fact] void should_not_receive_items_after_disposal() => _itemReceived.ShouldBeFalse();
}
