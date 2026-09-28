// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_querying_with_named_tags;

public class and_the_name_hash_matches_but_name_bytes_do_not : given.an_event_sequence_storage
{
    EventCount _count;

    async Task Because()
    {
        SeedEvent(EventSequenceNumber.First);
        await using (var context = CreateContext())
        {
            context.NamedTags.Add(new NamedTagEntry
            {
                EventSequenceId = _tableName,
                SequenceNumber = EventSequenceNumber.First.Value,
                Name = Encoding.UTF8.GetBytes("other"),
                NameHash = SHA256.HashData(Encoding.UTF8.GetBytes("account")),
                Value = Encoding.UTF8.GetBytes("one"),
                ValueHash = SHA256.HashData(Encoding.UTF8.GetBytes("one"))
            });
            await context.SaveChangesAsync();
        }

        _count = await _storage.GetCountMatching(new() { NamedTags = [new NamedTagCriterion(new TagName("account"))] });
    }

    [Fact] void should_not_trust_the_hash_without_comparing_name_bytes() => _count.Value.ShouldEqual(0UL);
}
