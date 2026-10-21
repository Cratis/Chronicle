// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences.for_Occurrences;

public class when_discovering_lifecycle_handlers_without_identity : given.lifecycle_occurrences
{
    void Establish() => _options.ActivationId = null;

    async Task Because() => _error = await Catch.Exception(_occurrences.Discover);

    [Fact] void should_fail_before_registration_effects() => _error.ShouldBeOfExactType<ActivationIdRequired>();
    [Fact] void should_not_record_any_lifecycle_work() => _recordings.ShouldBeEmpty();
}
