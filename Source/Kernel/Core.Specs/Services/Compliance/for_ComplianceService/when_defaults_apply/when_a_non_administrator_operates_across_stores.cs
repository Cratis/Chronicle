// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Contracts.Compliance;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Services.Compliance.for_ComplianceService.when_defaults_apply;

public class when_a_non_administrator_operates_across_stores : given.a_compliance_boundary
{
    void Establish()
    {
        Configure(enforce: false, useDefaults: true);
        HoldIdentifierOnlyIn(EventStore, OtherEventStore);
        Deny(OtherEventStore);
        Deny(ThirdEventStore);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_apply_the_requested_store_and_report_every_denied_other_store(bool allowNew)
    {
        (await Operate(allowNew)).ShouldEqual([OtherEventStore.Value, ThirdEventStore.Value]);
        ShouldNotHaveTouched(OtherEventStore);
        ShouldNotHaveTouched(ThirdEventStore);
        if (allowNew) await _keyStore.Received(1).AllowNewKeyFor(EventStore, EventStoreNamespace, Identifier);
        else await _keyStore.Received(1).DeleteFor(EventStore, EventStoreNamespace, Identifier);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task should_return_identical_results_for_identifiers_present_and_absent_in_denied_stores(bool allowNew, bool legacy)
    {
        var absent = (EncryptionKeyIdentifier)"absent-identifier";
        _keyStore.HasFor(OtherEventStore, EventStoreNamespace, absent, Arg.Any<EncryptionKeyRevision?>()).Returns(false);
        _keyStore.GetErasureFor(OtherEventStore, EventStoreNamespace, absent).Returns((EncryptionKeyErasure?)null);
        var presentResult = await Result(Identifier, allowNew, legacy);
        var absentResult = await Result(absent, allowNew, legacy);
        presentResult.ShouldEqual(absentResult);
        string.Join(' ', _audit.Values).ShouldNotContain(Identifier.Value);
        string.Join(' ', _audit.Values).ShouldNotContain(absent.Value);
        ShouldNotHaveTouched(OtherEventStore);
        ShouldNotHaveTouched(ThirdEventStore);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_not_change_coverage_when_a_denied_store_check_fails(bool allowNew)
    {
        FailChecksFor(ThirdEventStore);
        (await Operate(allowNew)).ShouldEqual([OtherEventStore.Value, ThirdEventStore.Value]);
        ShouldNotHaveTouched(ThirdEventStore);
        string.Join(' ', _audit.Values).ShouldNotContain("sensitive lookup detail");
    }

    [Fact]
    public async Task should_rate_limit_gap_warnings_without_changing_coverage()
    {
        await Operate(allowNew: false);
        await Operate(allowNew: false);
        _audit.InformationEntries.ShouldEqual(2);
        _audit.WarningEntries.ShouldEqual(1);
    }

    async Task<string> Result(EncryptionKeyIdentifier identifier, bool allowNew, bool legacy)
    {
        var delete = new DeleteEncryptionKeyRequest { EventStore = EventStore, Namespace = EventStoreNamespace, Identifier = identifier };
        var allow = new AllowNewEncryptionKeyRequest { EventStore = EventStore, Namespace = EventStoreNamespace, Identifier = identifier };
        if (!legacy)
        {
            return allowNew
                ? JsonSerializer.Serialize(await _service.AllowNewEncryptionKeyWithCoverage(allow, _context))
                : JsonSerializer.Serialize(await _service.DeleteEncryptionKeyWithCoverage(delete, _context));
        }
        await (allowNew ? _service.AllowNewEncryptionKey(allow, _context) : _service.DeleteEncryptionKey(delete, _context));
        return "success";
    }
}
