// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_saving;

public class and_two_sources_claim_the_same_value : given.a_unique_constraints_storage
{
    Exception? _error;
    bool _holderIsAllowed;
    bool _claimantIsAllowed;
    int _holders;

    async Task Establish() => await _storage.Save("holder", _definition, 42UL, "shared");

    async Task Because()
    {
        _error = await Catch.Exception(() => _storage.Save("claimant", _definition, 43UL, "shared"));
        (_holderIsAllowed, _) = await _storage.IsAllowed("holder", _definition, "shared");
        (_claimantIsAllowed, _) = await _storage.IsAllowed("claimant", _definition, "shared");
        await using var command = _connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM \"event-log_unique-name_constraint\"";
        _holders = Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    [Fact] void should_report_the_rejected_claim() => _error.ShouldBeOfExactType<DuplicateUniqueConstraintValue>();
    [Fact] void should_keep_the_holder() => _holderIsAllowed.ShouldBeTrue();
    [Fact] void should_refuse_the_other_source() => _claimantIsAllowed.ShouldBeFalse();
    [Fact] void should_keep_only_one_holder() => _holders.ShouldEqual(1);
}
