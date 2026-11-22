// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints.V2;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorageV2.given;

public abstract partial class a_sql_v2_storage
{
    [Fact]
    public async Task should_isolate_case_and_padding_distinct_sequences()
    {
        await Seed();
        foreach (var sequence in new[] { "LOG", "log ", "quote\";--", new string('界', 300) })
        {
            var other = Handle(sequence);
            ShouldRefuse(await other.Save(_owner, _definition, 99, _value, _scope, _authority), UniqueConstraintWriteFailure.MissingAuthority);
            (await other.InitializeAuthority(_definition.Name, _authority)).IsSuccess.ShouldBeTrue();
            (await other.Save(_otherOwner, _definition, 11, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
            (await other.IsAllowed(_otherOwner, _definition, _value, _scope)).IsAllowed.ShouldBeTrue();
        }

        await ShouldRetainClaim();
    }

    [Fact]
    public async Task should_isolate_long_quoted_multibyte_and_case_distinct_constraint_names()
    {
        await Seed();
        var prefix = new string('界', 300);
        foreach (var name in new[] { "CONSTRAINT", "constraint ", "a\"b';--", prefix + "a", prefix + "b" })
        {
            var definition = _definition with { Name = name };
            (await _storage.InitializeAuthority(definition.Name, _authority)).IsSuccess.ShouldBeTrue();
            (await _storage.Save(_otherOwner, definition, 11, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
            (await _storage.IsAllowed(_owner, definition, _value, _scope)).IsAllowed.ShouldBeFalse();
            (await _storage.RemoveAllFor(definition, _authority)).IsSuccess.ShouldBeTrue();
            await ShouldRetainClaim();
        }
    }

    [Fact]
    public async Task should_reuse_a_binding_across_independent_storage_handles()
    {
        await Seed();
        var other = Handle();
        (await other.Save(_owner, _definition, 99, _value, _scope, _authority, "updated-subject")).IsSuccess.ShouldBeTrue();
        (await other.IsAllowed(_owner, _definition, _value, _scope)).SequenceNumber.ShouldEqual((Concepts.Events.EventSequenceNumber)10);
        await ShouldRetainClaim();
    }
}
