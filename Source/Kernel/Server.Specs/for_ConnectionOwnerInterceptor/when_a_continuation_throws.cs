// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Server.for_ConnectionOwnerInterceptor;

public class when_a_continuation_throws : given.an_interceptor
{
    readonly ClaimsPrincipal _previous = new();
    readonly InvalidOperationException _failure = new();
    Exception _error;

    void Establish() => _owners.Principal = _previous;

    async Task Because()
    {
        try
        {
            await _interceptor.UnaryServerHandler<string, string>("request", _context, async (_, _) =>
            {
                await Task.Yield();
                _duringCall = _owners.Principal;
                throw _failure;
            });
        }
        catch (InvalidOperationException exception)
        {
            _error = exception;
        }
    }

    [Fact] void should_capture_the_callers_principal() => _duringCall.ShouldBeSame(_caller);
    [Fact] void should_restore_the_previous_holder() => _owners.Principal.ShouldBeSame(_previous);
    [Fact] void should_preserve_the_exception() => _error.ShouldBeSame(_failure);
}
