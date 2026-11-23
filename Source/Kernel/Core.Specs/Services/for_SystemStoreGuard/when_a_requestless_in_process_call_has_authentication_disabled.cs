// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Services.for_SystemStoreGuard;

public class when_a_requestless_in_process_call_has_authentication_disabled : given.an_in_process_request
{
    protected override Authentication AuthenticationOptions => new() { Enabled = false };

    protected override bool HasAmbientRequest => false;

    async Task Because() => await Evaluate();

    [Fact] void should_allow_the_requestless_call() => _allowed.ShouldBeTrue();
    [Fact] void should_not_refuse_the_system_store() => _refusal.ShouldBeNull();
}
