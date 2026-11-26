// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Services.for_SystemStoreGuard;

public class when_authentication_is_disabled_for_an_in_process_http_request : given.an_in_process_request
{
    protected override Authentication AuthenticationOptions => new() { Enabled = false };

    void Establish() => _httpContext.User = CallerNamed(Anonymous);

    async Task Because() => await Evaluate();

    [Fact] void should_allow_the_caller() => _allowed.ShouldBeTrue();
    [Fact] void should_not_refuse_the_system_store() => _refusal.ShouldBeNull();
}
