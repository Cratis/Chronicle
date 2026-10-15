// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_reading_effect_receipts;

public class without_prior_namespace_access : given.a_receipt_storage
{
    EffectReceipt? _receipt;

    async Task Because() => _receipt = await _storage.GetEffectReceipt(_receiptId);

    [Fact] void should_find_no_receipt_in_the_fresh_namespace() => _receipt.ShouldBeNull();
}
