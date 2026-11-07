// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt;

public class and_the_occurrence_is_application_scoped : given.an_attempt_with_a_delivery_hook
{
    Task Because() => Run(new(true, null));

    [Fact] void should_not_enter_a_delivery_scope() => _log.ShouldEqual(["activated:"]);
}
