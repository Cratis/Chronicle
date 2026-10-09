// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_a_namespace_is_added;

public class with_a_stopped_capture : given.a_captures_manager
{
    async Task Because() => await _manager.NamespaceAdded("new-tenant");

    [Fact] void should_not_subscribe_anything() => _subscriptions.DidNotReceive().Subscribe(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<CaptureDefinition>());
}
