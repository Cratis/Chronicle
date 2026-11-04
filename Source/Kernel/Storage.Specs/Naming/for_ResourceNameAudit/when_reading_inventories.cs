// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameAudit;

public class when_reading_inventories : given.an_audit
{
    void Establish() => Returns(Use("orders", "db:orders"), Use("orders", "db:orders-main", "main"));

    async Task Because() => await Run();

    [Fact] void should_not_create_anything() => _gate.DidNotReceiveWithAnyArgs().EnsureCanCreate(default!);
    [Fact] void should_not_reserve_anything() => _reservations.ReceivedCalls().ShouldBeEmpty();
}
