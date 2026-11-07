// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.StoreKeys.for_UniqueConstraintEncodingStorage;

public class when_reading_an_absent_key : given.a_key_storage
{
    UniqueConstraintEncodingKey? _first;
    UniqueConstraintEncodingKey? _second;
    UniqueConstraintEncodingKey _candidate;
    UniqueConstraintEncodingKey _created;

    void Establish() => _candidate = UniqueConstraintEncodingKey.Create();

    async Task Because()
    {
        _first = await _storage.GetKey();
        _second = await _storage.GetKey();
        _created = await _storage.CreateKeyIfAbsent(_candidate);
    }

    [Fact] void should_report_absence() => _first.ShouldBeNull();
    [Fact] void should_remain_absent_after_reading() => _second.ShouldBeNull();
    [Fact] void should_leave_the_slot_available_for_explicit_creation() => _created.ShouldEqual(_candidate);
}
