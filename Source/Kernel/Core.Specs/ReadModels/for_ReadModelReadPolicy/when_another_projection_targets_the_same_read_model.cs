// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;
using Grpc.Core;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReadPolicy;

public class when_another_projection_targets_the_same_read_model : given.a_read_policy
{
    void Establish()
    {
        _definition = _definition with { ObserverIdentifier = _supported.Identifier.Value };
        Producers(_supported, _unsupported);
    }

    async Task Because() => _error = await Catch.Exception(() => ReadModelReadPolicy.EnsureReadable(_grains, "store", _definition));

    [Fact] void should_refuse_the_read() => ((RpcException)_error!).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_use_the_neutral_refusal() => ((RpcException)_error!).Status.Detail.ShouldEqual(SystemStoreAccess.Refused);
}
