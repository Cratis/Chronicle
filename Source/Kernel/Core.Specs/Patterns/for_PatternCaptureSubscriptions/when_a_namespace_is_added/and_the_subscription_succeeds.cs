// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Patterns.for_PatternCaptureSubscriptions.when_a_namespace_is_added;

public class and_the_subscription_succeeds : given.a_namespace_subscription
{
    Task Because() => _onNamespaceAdded(_added);

    [Fact] async Task should_subscribe_capture_in_the_added_namespace() => await _capture.Received(1).Subscribe(_added.EventStore, _added.Namespace);
}
