// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.for_CaptureEventsNamespaceSubscriptions.when_a_namespace_is_added;

public class and_the_manager_succeeds : given.a_namespace_subscription
{
    async Task Because() => await _onNamespaceAdded(_added);

    [Fact] void should_tell_the_captures_manager_of_the_event_store() => _manager.Received(1).NamespaceAdded(_added.Namespace);
}
