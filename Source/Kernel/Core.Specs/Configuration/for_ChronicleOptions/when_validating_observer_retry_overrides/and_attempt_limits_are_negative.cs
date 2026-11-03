// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_validating_observer_retry_overrides;

public class and_attempt_limits_are_negative : given.configured_retry_options
{
    [Fact] void should_reject_an_explicit_negative_limit() => Validate(new ObserverRetryOverrides { MaxRetryAttempts = -1 }).Single().MemberNames.Single().ShouldEqual($"ObserverRetryOverrides['{Key}'].MaxRetryAttempts");
    [Fact] void should_reject_an_inherited_negative_limit() => Validate(new ObserverRetryOverrides(), new Observers { MaxRetryAttempts = -1 }).Length.ShouldEqual(1);
    [Fact] void should_allow_replacing_an_invalid_global_limit() => Validate(new ObserverRetryOverrides { MaxRetryAttempts = 0 }, new Observers { MaxRetryAttempts = -1 }).Length.ShouldEqual(0);
}
