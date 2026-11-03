// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverJobAdmission.when_creating;

public class with_rules_sharing_an_order : given.rules
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => ObserverJobAdmission.Ordered([Rule(100, true), Rule(200, true), Rule(100, true)]));

    [Fact] void should_refuse_the_rules() => _error.ShouldBeOfExactType<DuplicateObserverJobAdmissionRuleOrder>();
}
