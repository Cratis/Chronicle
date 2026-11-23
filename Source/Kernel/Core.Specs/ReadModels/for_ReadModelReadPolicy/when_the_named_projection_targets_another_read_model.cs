// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;
using Grpc.Core;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReadPolicy;

public class when_the_named_projection_targets_another_read_model : given.a_read_policy
{
    void Establish()
    {
        _definition = _definition with { ObserverIdentifier = _unsupported.Identifier.Value };
        _manager.GetProjectionDefinitions().Returns([_supported, _unsupported with { ReadModel = "another-read-model" }]);
    }

    async Task Because() => _error = await Catch.Exception(() => ReadModelReadPolicy.EnsureReadable(_grains, "store", _definition));

    [Fact] void should_refuse_the_read() => ((RpcException)_error!).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_use_the_neutral_refusal() => ((RpcException)_error!).Status.Detail.ShouldEqual(SystemStoreAccess.Refused);
}
