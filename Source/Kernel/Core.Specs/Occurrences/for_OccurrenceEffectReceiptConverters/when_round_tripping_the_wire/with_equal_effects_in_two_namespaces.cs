// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;
using ProtoBuf;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceEffectReceiptConverters.when_round_tripping_the_wire;

public class with_equal_effects_in_two_namespaces : Specification
{
    Contracts.Occurrences.OccurrenceEffectReceipt[] _entries;

    void Because()
    {
        var receipt = EffectReceiptConverters.ToDetails(new("same-effect", new("occurrence", 4), EffectOutcome.NoEvents, null, null, null, [], "{\"answer\":42}", DateTimeOffset.UnixEpoch));
        _entries = new[] { "tenant", "other-tenant" }.Select(name => Serializer.DeepClone(new OccurrenceEffectReceipt(name, receipt).ToContract())).ToArray();
    }

    [Fact] void should_preserve_both_namespace_names() => _entries.Select(entry => entry.Namespace).ShouldContainOnly("tenant", "other-tenant");
    [Fact] void should_preserve_the_equal_effect_identities() => _entries.Select(entry => entry.Receipt.Id).ShouldEqual(["same-effect", "same-effect"]);
    [Fact] void should_preserve_the_recorded_response() => _entries.Select(entry => entry.Receipt.Response).Distinct().ShouldContainOnly("{\"answer\":42}");
    [Fact] void should_preserve_the_receipt_epoch() => _entries.Select(entry => entry.Receipt.Epoch).Distinct().ShouldContainOnly(4L);
}
