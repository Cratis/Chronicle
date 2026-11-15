// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_validating_federation;

public class and_nothing_is_configured : Specification
{
    bool _enabled;

    void Because() => _enabled = new ChronicleOptions().Federation.Enabled;

    [Fact] void should_leave_federation_off_by_default() => _enabled.ShouldBeFalse();
}
