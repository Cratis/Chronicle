// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_validating_observer_retry_overrides;

[Collection("configuration bootstrap")]
public class and_keys_are_invalid : given.configured_retry_options
{
    [Theory]
    [InlineData("")]
    [InlineData("observer")]
    [InlineData("observer#store#namespace")]
    [InlineData("observer#store#namespace#sequence#extra")]
    [InlineData("observer##namespace#sequence")]
    [InlineData("observer#store# #sequence")]
    [InlineData("observer#store#namespace#sequence:child")]
    public void should_report_the_key(string key) => Validate(new ObserverRetryOverrides(), key: key).Single().MemberNames.Single().ShouldEqual($"ObserverRetryOverrides['{key}']");

    [Fact] void should_fail_real_startup_validation_for_an_invalid_bound_entry() => Catch.Exception(() => Configure("""
        { "Cratis": { "Chronicle": { "ObserverRetryOverrides": { "bare-observer": { "MaxRetryAttempts": 2 } } } } }
        """)).ShouldBeOfExactType<OptionsValidationException>();

    [Fact] void should_reject_a_null_map() => Validate(new ChronicleOptions { ObserverRetryOverrides = null! }).Single().MemberNames.Single().ShouldEqual("ObserverRetryOverrides");
    [Fact] void should_reject_a_null_entry() => Validate((ObserverRetryOverrides)null!).Single().MemberNames.Single().ShouldEqual($"ObserverRetryOverrides['{Key}']");
}
