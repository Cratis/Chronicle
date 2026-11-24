// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;
using Grpc.Core;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReadPolicy;

public class when_a_system_projection_and_a_reducer_target_it : given.a_read_policy
{
    void Establish()
    {
        Producers(_unsupported);
        _reducers.HasReducerForReadModel(_definition.Identifier).Returns(true);
    }

    async Task Because() => _error = await Catch.Exception(() => ReadModelReadPolicy.EnsureReadable(_grains, "store", _definition));

    [Fact] void should_refuse_the_read() => ((RpcException)_error!).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_use_the_neutral_refusal() => ((RpcException)_error!).Status.Detail.ShouldEqual(SystemStoreAccess.Refused);
}
