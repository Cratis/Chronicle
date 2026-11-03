// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ConfigurationForObserverProvider;

public class when_only_another_identity_matches : given.a_provider
{
    [Theory]
    [InlineData("other#store#namespace#sequence")]
    [InlineData("observer#other#namespace#sequence")]
    [InlineData("observer#store#other#sequence")]
    [InlineData("observer#store#namespace#other")]
    [InlineData("Observer#store#namespace#sequence")]
    [InlineData("observer")]
    public async Task should_not_match_a_different_identity_even_with_a_case_insensitive_dictionary(string key)
    {
        Configure(new Dictionary<string, ObserverRetryOverrides>(StringComparer.OrdinalIgnoreCase) { [key] = new() { MaxRetryAttempts = 1 } });
        var result = await _provider.GetFor(ObserverKey);
        ReferenceEquals(result, _defaults).ShouldBeTrue();
    }
}
