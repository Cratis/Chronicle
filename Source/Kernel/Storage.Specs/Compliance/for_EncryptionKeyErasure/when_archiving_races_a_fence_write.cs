// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_EncryptionKeyErasure;

public class when_archiving_races_a_fence_write : Specification
{
    static readonly EventStoreName _store = "store";
    static readonly EventStoreNamespaceName _namespace = "namespace";
    static readonly EncryptionKeyIdentifier _identifier = "subject";
    InMemoryEncryptionKeyStorage _authority;
    IEncryptionKeyFenceRecords _records;
    EncryptionKeyFenceState _observed;
    EncryptionKeyFenceState _current;
    bool _removed;
    bool _archiveAcknowledged;

    async Task Establish()
    {
        _authority = new();
        var initial = await _authority.ReadFor(_store, _namespace, _identifier);
        _observed = initial with { Erasure = initial.Erasure with { Version = 1, Requests = [new("confirmed", Confirmed: true)] } };
        await _authority.CompareExchangeFor(_store, _namespace, _identifier, initial, _observed);
        _records = Substitute.For<IEncryptionKeyFenceRecords>();
        _records.ReadRecord(_store, _namespace, Arg.Any<string>()).Returns(call => _authority.ReadRecord(_store, _namespace, call.Arg<string>()));
        _records.WriteRecord(_store, _namespace, Arg.Any<string>(), Arg.Any<string>()).Returns(async call =>
        {
            await _authority.WriteRecord(_store, _namespace, call.ArgAt<string>(2), call.ArgAt<string>(3));
            _archiveAcknowledged = true;
            var competing = _observed with { Erasure = _observed.Erasure with { Version = 2, Requests = [.. _observed.Erasure.Requests ?? [], new("pending")] } };
            await _authority.CompareExchangeFor(_store, _namespace, _identifier, _observed, competing);
        });
    }

    async Task Because()
    {
        var compacted = await EncryptionKeyRequestArchive.Prepare(_records, _store, _namespace, _identifier, _observed.Erasure, retain: 0);
        _removed = await _authority.CompareExchangeFor(_store, _namespace, _identifier, _observed, _observed with { Erasure = compacted });
        _current = await _authority.ReadFor(_store, _namespace, _identifier);
    }

    [Fact] void should_acknowledge_the_archive_before_attempting_removal() => _archiveAcknowledged.ShouldBeTrue();
    [Fact] void should_not_remove_requests_using_the_stale_version() => _removed.ShouldBeFalse();
    [Fact] void should_preserve_the_competing_request() => _current.Erasure.Requests.Select(_ => _.RequestId).ShouldEqual(["confirmed", "pending"]);
}
