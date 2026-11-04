// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Kernel;
using Cratis.Chronicle.Statistics;

namespace Cratis.Chronicle.Projections.for_KernelProjections;

public class when_the_signature_cannot_be_computed : given.kernel_projections
{
    ProjectionDefinition[] _definitions;

    void Establish()
    {
        var definition = KernelProjectionDeclarations.Lower(typeof(EventTypeStatisticsProjection)).Projections.Single();
        _definitions = [definition with { InitialModelState = new JsonObject { ["invalidNumber"] = double.NaN } }];
    }

    Task Because() => _kernel.RegisterProjections(EventStore, _definitions);

    [Fact] void should_register_the_original_definitions() => _manager.Received(1).Register(_definitions, ProjectionOwner.Kernel, null, null);
    [Fact] void should_not_call_shared_registration() => _manager.DidNotReceiveWithAnyArgs().RegisterShared(default!, default!);
}
