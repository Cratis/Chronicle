// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_MongoDBReservedEventSequenceIds;

public class when_checking_every_collection_name_the_storage_opens : Specification
{
    /// <summary>
    /// Changeset collections are opt-in and their suffix is intentionally not reserved, see when_checking.
    /// </summary>
    const string AllowedChangesetSuffix = "-changes";
    readonly MongoDBReservedEventSequenceIds _reserved = new();
    HashSet<string> _names;
    HashSet<string> _tails;
    HashSet<string> _typeDerivedCalls;

    void Because() => (_names, _tails, _typeDerivedCalls) = given.StorageSource.Scan();

    [Fact] void should_find_collection_names_to_check() => _names.ShouldContain("encryption-keys");

    [Fact] void should_reserve_every_named_collection() => _names.Where(name => !IsReserved(name)).ShouldBeEmpty();

    [Fact]
    void should_reserve_every_derived_collection_pattern() => _tails
        .Where(tail => tail != AllowedChangesetSuffix && !IsReserved($"orders{tail}"))
        .ShouldBeEmpty();

    [Fact] void should_not_open_collections_with_type_derived_names() => _typeDerivedCalls.ShouldBeEmpty();

    bool IsReserved(string name) => _reserved.IsReserved(new Concepts.EventSequences.EventSequenceId(name));
}
