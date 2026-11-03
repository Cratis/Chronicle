// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventGenerationRelease.when_releasing;

public class and_an_older_stored_lower_pin_is_plaintext : given.a_downcast_protected_token
{
    void Establish() => _event = _event with { GenerationalContent = new Dictionary<int, string> { [1] = _plaintext.ToJsonString(), [3] = _protectedContent.ToJsonString() } };

    async Task Because() => _result = await Release();

    [Fact] void should_preserve_legacy_plaintext_release() => ReleasedToken.ShouldEqual(Token);
}
