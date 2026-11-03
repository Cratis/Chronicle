// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ConfigurationForObserverProvider;

public class when_no_override_matches : given.a_provider
{
    async Task Because() => _result = await _provider.GetFor(ObserverKey);

    [Fact] void should_return_the_original_global_settings() => ReferenceEquals(_result, _defaults).ShouldBeTrue();
}
