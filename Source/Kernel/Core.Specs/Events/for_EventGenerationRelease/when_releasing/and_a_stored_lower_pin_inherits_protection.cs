// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventGenerationRelease.when_releasing;

public class and_a_stored_lower_pin_inherits_protection : given.a_downcast_protected_token
{
    async Task Because() => _result = await Release();

    [Fact] void should_deliver_plaintext() => ReleasedToken.ShouldEqual(Token);
    [Fact] void should_keep_stored_content_unchanged() => _result[0].GenerationalContent.ShouldBeSame(_event.GenerationalContent);
}
