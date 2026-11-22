// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Server.Authentication.Controllers.for_AuthorizationController.given;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Chronicle.Server.Authentication.Controllers.for_AuthorizationController;

/// <summary>
/// Characterizes today's behavior: the token is issued only once the authentication audit event has been appended, so
/// a saturated event log holds every client's authentication back.
/// </summary>
public class when_exchanging_client_credentials_while_the_event_log_does_not_answer : a_client_credentials_token_endpoint
{
    readonly TaskCompletionSource<AppendResult> _append = new();
    Task<IActionResult> _exchange;
    bool _completed;

    void Establish()
    {
        var application = new object();
#pragma warning disable CA2012 // Configure substitute returns; these calls do not perform asynchronous work.
        _applicationManager.FindByClientIdAsync(ClientId, Arg.Any<CancellationToken>()).Returns(new ValueTask<object?>(application));
        _applicationManager.GetIdAsync(application, Arg.Any<CancellationToken>()).Returns(new ValueTask<string?>(Guid.NewGuid().ToString()));
        _applicationManager.GetClientIdAsync(application, Arg.Any<CancellationToken>()).Returns(new ValueTask<string?>(ClientId));
#pragma warning restore CA2012
        _eventLog.Append(Arg.Any<EventSourceId>(), Arg.Any<object>()).Returns(_append.Task);
    }

    async Task Because()
    {
        _exchange = _controller.Exchange();
        try
        {
            await _exchange.WaitAsync(TimeSpan.FromMilliseconds(500));
        }
        catch (TimeoutException)
        {
            // Expected: the event log never answers.
        }

        _completed = _exchange.IsCompleted;
    }

    void Destroy() => _append.TrySetResult(default!);

    [Fact] void should_not_issue_the_token_until_the_event_log_answers() => _completed.ShouldBeFalse();
}
