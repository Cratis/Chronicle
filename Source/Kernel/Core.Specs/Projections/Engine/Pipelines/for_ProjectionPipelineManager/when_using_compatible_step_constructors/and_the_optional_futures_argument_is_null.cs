// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures.given;
using Cratis.Chronicle.Storage.Changes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager.when_using_compatible_step_constructors;

public class and_the_optional_futures_argument_is_null : a_resolve_futures_step
{
    ProjectionEventContext _positional;
    ProjectionEventContext _named;

    async Task Because()
    {
        var storage = Substitute.For<IChangesetStorage>();
        var positional = new SaveChanges(_sink, storage, true, NullLogger<SaveChanges>.Instance, null);
        var named = new SaveChanges(_sink, storage, true, NullLogger<SaveChanges>.Instance, projectionFutures: null, changesetContainer: null);
        _positional = await positional.Perform(_projection, _context);
        _named = await named.Perform(_projection, _context);
    }

    [Fact] void should_accept_a_positional_null_without_changing_empty_processing() => _positional.ShouldBeSame(_context);
    [Fact] void should_accept_a_named_null_without_changing_empty_processing() => _named.ShouldBeSame(_context);
}
