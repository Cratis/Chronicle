// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.for_SystemStoreGuard;

public class when_an_administrator_reads_system_with_an_ambient_request : given.an_in_process_request
{
    async Task Because() => await Evaluate();

    [Fact] void should_use_the_ambient_request() => _resolvedRequest.ShouldEqual(_httpContext);
    [Fact] void should_allow_the_administrator() => _allowed.ShouldBeTrue();
    [Fact] void should_not_refuse_the_system_store() => _refusal.ShouldBeNull();
}
