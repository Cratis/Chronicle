// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_PatternMiner.when_mining;

/// <summary>
/// A namespace is a tenant. The same scope name in two namespaces is two different tenants' behavior, and
/// counting them into one sketch would leak one tenant's routine into what the other tenant is told about
/// its own users.
/// </summary>
public class the_same_scope_in_two_namespaces : given.a_pattern_miner
{
    EventStoreNamespaceName _otherNamespace;
    IEnumerable<BehaviorPattern> _forFirstNamespace;
    IEnumerable<BehaviorPattern> _forOtherNamespace;

    void Establish() => _otherNamespace = "some-other-tenant";

    void Because()
    {
        for (var count = 0; count < 20; count++)
        {
            _miner.Observe(_eventStore, _namespace, Features("user-42", "ApproveExpenseReport"));
        }

        for (var count = 0; count < 20; count++)
        {
            _miner.Observe(_eventStore, _otherNamespace, Features("user-42", "SubmitExpenseReport", DayOfWeek.Friday, TimeBucket.Evening));
        }

        _forFirstNamespace = _miner.GetSurvivingPatterns(_eventStore, _namespace, "user-42");
        _forOtherNamespace = _miner.GetSurvivingPatterns(_eventStore, _otherNamespace, "user-42");
    }

    [Fact] void should_not_leak_the_other_tenant_behavior_into_the_first() =>
        _forFirstNamespace.All(_ => _.Facets.ValueOf(FacetName.CommandType) != new FacetValue("SubmitExpenseReport")).ShouldBeTrue();

    [Fact] void should_not_leak_the_first_tenant_behavior_into_the_other() =>
        _forOtherNamespace.All(_ => _.Facets.ValueOf(FacetName.CommandType) != new FacetValue("ApproveExpenseReport")).ShouldBeTrue();

    [Fact] void should_count_only_the_tenant_own_observations() =>
        _forFirstNamespace.Concat(_forOtherNamespace).All(_ => _.Occurrences.Value == 20L).ShouldBeTrue();
}
