// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStores.for_ReservedEventStoreNames;

public class when_checking_names_with_a_prefix : Specification
{
    bool _splitAdmin;
    bool _splitConfig;
    bool _splitLocalUpper;
    bool _ordinary;
    bool _noPrefix;
    bool _reservedWithPrefix;

    void Because()
    {
        _splitAdmin = ReservedEventStoreNames.IsReserved("min", prefix: "ad");
        _splitConfig = ReservedEventStoreNames.IsReserved("fig", prefix: "con");
        _splitLocalUpper = ReservedEventStoreNames.IsReserved("CAL", prefix: "lo");
        _ordinary = ReservedEventStoreNames.IsReserved("es1", prefix: "run_");
        _noPrefix = ReservedEventStoreNames.IsReserved("min");
        _reservedWithPrefix = ReservedEventStoreNames.IsReserved("a+b", prefix: "run_");
    }

    [Fact] void should_reject_a_prefix_that_completes_admin() => _splitAdmin.ShouldBeTrue();
    [Fact] void should_reject_a_prefix_that_completes_config() => _splitConfig.ShouldBeTrue();
    [Fact] void should_ignore_casing() => _splitLocalUpper.ShouldBeTrue();
    [Fact] void should_accept_ordinary_names() => _ordinary.ShouldBeFalse();
    [Fact] void should_accept_a_partial_name_without_a_prefix() => _noPrefix.ShouldBeFalse();
    [Fact] void should_still_reject_a_plus() => _reservedWithPrefix.ShouldBeTrue();
}
