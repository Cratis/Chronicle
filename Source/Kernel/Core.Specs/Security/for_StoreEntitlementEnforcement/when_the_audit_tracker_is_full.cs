// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Security.for_StoreEntitlementEnforcement;

public class when_the_audit_tracker_is_full : Specification
{
    static readonly DateTimeOffset _start = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
    readonly NullLoggerFactory _loggers = new();
    bool _freshKeyWhileEntriesAreCurrent;
    bool _freshKeyAfterEntriesExpired;

    static (PrincipalKey Holder, string Store, string? Namespace, string Capability) Key(int index) => (new(PrincipalKey.LocalIssuer, $"subject-{index}"), "orders", "tenant", "read");

    void Establish()
    {
        for (var index = 0; index < 10_000; index++) StoreEntitlementEnforcement.TakeSlot(_loggers, Key(index), _start);
    }

    void Because()
    {
        _freshKeyWhileEntriesAreCurrent = StoreEntitlementEnforcement.TakeSlot(_loggers, Key(10_000), _start.AddMinutes(1));
        _freshKeyAfterEntriesExpired = StoreEntitlementEnforcement.TakeSlot(_loggers, Key(10_001), _start.AddMinutes(11));
    }

    [Fact] void should_not_record_a_new_key_while_every_entry_is_current() => _freshKeyWhileEntriesAreCurrent.ShouldBeFalse();
    [Fact] void should_record_a_new_key_once_entries_have_expired() => _freshKeyAfterEntriesExpired.ShouldBeTrue();
}
