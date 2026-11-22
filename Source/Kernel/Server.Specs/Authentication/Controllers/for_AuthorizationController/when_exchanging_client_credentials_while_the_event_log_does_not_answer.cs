// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Server.Authentication.Controllers.for_AuthorizationController.given;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Chronicle.Server.Authentication.Controllers.for_AuthorizationController;

/// <summary>
/// The authentication audit event is appended through the event log grain, so a saturated event log must not hold every
/// client's authentication back: the token is issued once the audit grace has passed, and the append carries on.
/// </summary>
public class when_exchanging_client_credentials_while_the_event_log_does_not_answer : a_client_credentials_token_endpoint
{
    readonly TaskCompletionSource<AppendResult> _append = new();
    Task<IActionResult> _exchange;
    bool _completed;
    IActionResult _result;

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
            await _exchange.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (TimeoutException)
        {
            // Reported by the assertion below.
        }

        _completed = _exchange.IsCompletedSuccessfully;
        _result = _completed ? await _exchange : null;
    }

    void Destroy() => _append.TrySetResult(default!);

    [Fact] void should_answer_without_waiting_for_the_event_log() => _completed.ShouldBeTrue();
    [Fact] void should_issue_the_token() => _result.ShouldBeOfExactType<SignInResult>();
}
