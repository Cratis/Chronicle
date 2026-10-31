// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_settling_claims;

[Collection(SqlServerCollection.Name)]
public class and_the_provider_is_sql_server(SqlServerFixture fixture) : given.a_claim_lifecycle
{
    async Task Establish()
    {
        var connectionString = await fixture.CreateDatabase();
        await CreateStorage(connectionString, true, () => fixture.DropDatabase(connectionString));
    }

    Task Because() => SettleClaims();

    [Fact] void should_report_the_rejected_claim() => _claimError.ShouldBeOfExactType<DuplicateUniqueConstraintValue>();
    [Fact] void should_keep_the_first_holder() => _holderAllowed.ShouldBeTrue();
    [Fact] void should_refuse_the_other_source() => _claimantAllowed.ShouldBeFalse();
    [Fact] void should_keep_only_one_claim() => _claimCount.ShouldEqual(1);
    [Fact] void should_report_the_rejected_move() => _moveError.ShouldBeOfExactType<DuplicateUniqueConstraintValue>();
    [Fact] void should_keep_the_old_claim_on_a_rejected_move() => _oldValueAvailable.ShouldBeFalse();
    [Fact] void should_keep_the_old_sequence_number() => _oldSequenceNumber.ShouldEqual((EventSequenceNumber)44UL);
    [Fact] void should_free_the_released_value() => _releasedValueAvailable.ShouldBeTrue();
}
