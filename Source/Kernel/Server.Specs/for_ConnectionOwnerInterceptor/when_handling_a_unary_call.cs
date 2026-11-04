// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_ConnectionOwnerInterceptor;

public class when_handling_a_unary_call : given.an_interceptor
{
    Task Because() => _interceptor.UnaryServerHandler<string, string>("request", _context, (_, _) =>
    {
        _duringCall = _owners.Principal;
        return Task.FromResult("response");
    });

    [Fact] void should_capture_the_callers_principal() => _duringCall.ShouldBeSame(_caller);
    [Fact] void should_clear_the_holder() => _owners.Principal.ShouldBeNull();
}
