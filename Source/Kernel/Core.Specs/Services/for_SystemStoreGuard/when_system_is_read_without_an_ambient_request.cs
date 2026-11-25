// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Services.for_SystemStoreGuard;

public class when_system_is_read_without_an_ambient_request : given.an_in_process_request
{
    protected override bool HasAmbientRequest => false;

    async Task Because() => await Evaluate();

    [Fact] void should_not_allow_an_absent_identity() => _allowed.ShouldBeFalse();
    [Fact] void should_refuse_the_system_store() => Refusal.StatusCode.ShouldEqual(StatusCode.PermissionDenied);
}
