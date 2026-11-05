// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSourceIds.for_spelling_variants.when_claiming_unique_values;

[Collection(SqlServerCollection.Name)]
public class on_sql_server(SqlServerFixture fixture) : given.a_spelling_storage(new("sql_server", fixture.CreateDatabase, fixture.DropDatabase))
{
    static readonly string _value = new('a', 64);
    UniqueConstraintDefinition _definition;
    Exception? _upperError;
    Exception? _paddedError;
    bool _upperAllowed;
    bool _paddedAllowed;
    bool _ownerAllowed;
    UniqueConstraintValue[] _upperValues;
    UniqueConstraintValue[] _ownerValues;
    bool _stillClaimedAfterRemoveValue;
    bool _stillClaimedAfterRemove;

    async Task Because()
    {
        _definition = new("claims", []) { Mode = UniqueConstraintMode.PerValue };
        await _harness.UniqueConstraints.Save("Alice", _definition, 1UL, _value);
        _upperError = await Catch.Exception(() => _harness.UniqueConstraints.Save("alice", _definition, 2UL, _value));
        _paddedError = await Catch.Exception(() => _harness.UniqueConstraints.Save("Alice ", _definition, 3UL, _value));
        _upperAllowed = (await _harness.UniqueConstraints.IsAllowed("alice", _definition, _value)).IsAllowed;
        _paddedAllowed = (await _harness.UniqueConstraints.IsAllowed("Alice ", _definition, _value)).IsAllowed;
        _ownerAllowed = (await _harness.UniqueConstraints.IsAllowed("Alice", _definition, _value)).IsAllowed;
        _upperValues = [.. await _harness.UniqueConstraints.GetFor("alice", _definition)];
        _ownerValues = [.. await _harness.UniqueConstraints.GetFor("Alice", _definition)];
        await _harness.UniqueConstraints.RemoveValue("alice", _definition, _value);
        _stillClaimedAfterRemoveValue = !(await _harness.UniqueConstraints.IsAllowed("bob", _definition, _value)).IsAllowed;
        await _harness.UniqueConstraints.Remove("alice", _definition);
        _stillClaimedAfterRemove = !(await _harness.UniqueConstraints.IsAllowed("bob", _definition, _value)).IsAllowed;
    }

    [Fact] void should_refuse_the_lower_case_spelling() => _upperError.ShouldBeOfExactType<DuplicateUniqueConstraintValue>();
    [Fact] void should_refuse_the_spelling_with_a_trailing_space() => _paddedError.ShouldBeOfExactType<DuplicateUniqueConstraintValue>();
    [Fact] void should_not_allow_the_lower_case_spelling() => _upperAllowed.ShouldBeFalse();
    [Fact] void should_not_allow_the_spelling_with_a_trailing_space() => _paddedAllowed.ShouldBeFalse();
    [Fact] void should_allow_the_owner() => _ownerAllowed.ShouldBeTrue();
    [Fact] void should_not_list_the_value_for_another_spelling() => _upperValues.Length.ShouldEqual(0);
    [Fact] void should_list_the_value_for_the_owner() => _ownerValues.Length.ShouldEqual(1);
    [Fact] void should_keep_the_claim_when_another_spelling_removes_the_value() => _stillClaimedAfterRemoveValue.ShouldBeTrue();
    [Fact] void should_keep_the_claim_when_another_spelling_removes_the_owner() => _stillClaimedAfterRemove.ShouldBeTrue();
}
