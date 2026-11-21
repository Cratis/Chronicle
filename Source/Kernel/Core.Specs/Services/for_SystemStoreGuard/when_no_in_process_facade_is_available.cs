// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Services.for_SystemStoreGuard;

public class when_no_in_process_facade_is_available : given.an_in_process_request
{
    void Establish() => SystemStoreGuard.InProcessServices = null;

    async Task Because() => await Evaluate();

    [Fact] void should_not_allow_the_caller() => _allowed.ShouldBeFalse();
    [Fact] void should_refuse_the_system_store() => Refusal.StatusCode.ShouldEqual(StatusCode.PermissionDenied);
}
