// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.Server.for_UserIdentityMiddleware;

public class when_an_authenticated_principal_has_only_a_subject : Specification
{
    const string Subject = "operator-id";

    RequestDelegate _next;
    DefaultHttpContext _context;
    UserIdentityMiddleware _middleware;
    string _subject;
    string _name;
    string _username;

    void Establish()
    {
        _next = _ =>
        {
            _subject = RequestContext.Get(WellKnownKeys.UserIdentity) as string;
            _name = RequestContext.Get(WellKnownKeys.UserName) as string;
            _username = RequestContext.Get(WellKnownKeys.UserPreferredUserName) as string;
            return Task.CompletedTask;
        };
        _context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", Subject)],
                authenticationType: "Test"))
        };
        _middleware = new UserIdentityMiddleware(_next);
    }

    async Task Because() => await _middleware.InvokeAsync(_context);

    [Fact] void should_flow_the_subject() => _subject.ShouldEqual(Subject);
    [Fact] void should_use_the_subject_as_the_name() => _name.ShouldEqual(Subject);
    [Fact] void should_use_the_subject_as_the_preferred_username() => _username.ShouldEqual(Subject);
}
