// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_reads_are_protected;

public class without_an_allowance : given.protected_read_surfaces
{
    Task Because() => Exercise();

    [Fact] void should_refuse_every_read_surface() => _errors.Values.ShouldEachConformTo(error => error is ReadRefusedWhileReadsAreProtected);
    [Fact] void should_refuse_before_transport() => _services.ReadModels.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_refuse_before_materialized_transport() => _services.MaterializedReadModels.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_cover_all_read_families() => _errors.Count.ShouldEqual(9);
}
