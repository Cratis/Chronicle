// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.for_CaptureEventsNamespaceSubscriptions.when_a_namespace_is_added;

public class and_the_manager_fails : given.a_namespace_subscription
{
    Exception _exception;

    void Establish() => _manager.NamespaceAdded(_added.Namespace).Returns(Task.FromException(new TimeoutException()));

    async Task Because() => _exception = await Catch.Exception(() => _onNamespaceAdded(_added));

    [Fact] void should_not_break_the_broadcast_channel() => _exception.ShouldBeNull();
}
