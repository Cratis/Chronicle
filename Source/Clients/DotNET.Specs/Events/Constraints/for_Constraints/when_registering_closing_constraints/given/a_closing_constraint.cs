// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.EventSequences;
using IContractsConstraints = Cratis.Chronicle.Contracts.Events.Constraints.IConstraints;

namespace Cratis.Chronicle.Events.Constraints.for_Constraints.when_registering_closing_constraints.given;

public class a_closing_constraint : for_Constraints.given.no_constraints
{
    protected IContractsConstraints _constraintsService;
    protected Exception _error;

    async Task Establish()
    {
        _constraintsService = Substitute.For<IContractsConstraints>();
        _services.Constraints.Returns(_constraintsService);
        _constraintsProvider.Provide().Returns(new IConstraintDefinition[]
        {
            new ClosesStreamConstraintDefinition("closing", _ => "closed", ["Closed"], ClosedStreamDimensions.EventSourceId, [])
        }.ToImmutableList());
        await _constraints.Discover();
    }
}
