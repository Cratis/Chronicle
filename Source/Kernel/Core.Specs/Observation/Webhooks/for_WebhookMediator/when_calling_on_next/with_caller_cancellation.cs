// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_calling_on_next;

public class with_caller_cancellation : given.a_cancellable_delivery
{
    async Task Because()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        _result = await _mediator.OnNext(_target, "partition", [], cancellationToken: cancellation.Token);
    }

    [Fact] void should_cancel_the_http_operation_before_returning() => _cancelled.ShouldBeTrue();
    [Fact] void should_not_report_success() => _result.IsSuccess.ShouldBeFalse();
}
