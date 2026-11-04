// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventGenerationRelease.when_releasing;

public class and_an_inherited_protected_subject_is_erased : given.a_downcast_protected_token
{
    async Task Establish() => await _keys.DeleteFor(Store, Namespace, Subject);

    async Task Because() => _result = await Release();

    [Fact] void should_not_deliver_the_token() => ReleasedToken.ShouldEqual(string.Empty);
}
