// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_an_older_version_registers;

public class TicketProjection : IProjectionFor<Ticket>
{
    public void Define(IProjectionBuilderFor<Ticket> builder) => builder
        .From<TicketOpened>(b => b.Set(m => m.Title).To(e => e.Title));
}
