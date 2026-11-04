// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_ConnectionOwnerInterceptor;

public class when_handling_an_unlisted_call : given.an_interceptor
{
    Task Because()
    {
        _context.Method.Returns("/Cratis.Chronicle.Contracts.EventSequences.EventSequences/Append");
        return _interceptor.UnaryServerHandler<string, string>("request", _context, (_, _) =>
        {
            _duringCall = _owners.Principal;
            return Task.FromResult("response");
        });
    }

    [Fact] void should_not_set_the_holder() => _duringCall.ShouldBeNull();
    [Fact] void should_leave_the_holder_empty() => _owners.Principal.ShouldBeNull();
}
