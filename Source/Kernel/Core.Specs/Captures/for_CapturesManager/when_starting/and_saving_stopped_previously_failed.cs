// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures.Engine;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_starting;

public class and_saving_stopped_previously_failed : given.a_manager_with_failing_stop
{
    IEnumerable<CaptureValidationMessage> _messages;

    Task Establish() => FailStop();

    async Task Because() => _messages = await _manager.Start(_events.Id);

    [Fact] void should_report_success() => _messages.ShouldBeEmpty();
    [Fact] async Task should_establish_subscriptions_before_reporting_success() => await _subscriptions.Received(1).Subscribe(_eventStore, Arg.Is<CaptureDefinition>(definition => definition.Id == _events.Id));
    [Fact] async Task should_keep_the_capture_started() => (await _captures.Get(_events.Id)).Status.ShouldEqual(CaptureStatus.Started);
}
