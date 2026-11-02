// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverJobAdmission.when_creating;

public class checking_the_rules_at_silo_start_with_rules_sharing_an_order : given.rules
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => new ObserverJobAdmissionRulesCheck([Rule(100, true), Rule(100, true)]).Check(CancellationToken.None));

    [Fact] void should_stop_the_silo_from_starting() => _error.ShouldBeOfExactType<DuplicateObserverJobAdmissionRuleOrder>();
}
