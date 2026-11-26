// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_valid_query_store_arguments_are_bound : given.a_query_operation_policy
{
    bool _caseInsensitive;
    bool _concept;
    bool _override;
    bool _ordinaryReplacement;
    bool _systemReplacement;

    void Establish() => _enforce = true;

    async Task Because()
    {
        _caseInsensitive = await Allows(nameof(given.operation_queries.Ordinary), new() { ["EVENTSTORE"] = "application" });
        _concept = await Allows(nameof(given.operation_queries.Destructive), new() { ["eventStore"] = new EventStoreName("application") });
        _override = await Allows(nameof(given.operation_queries.Optional), new() { ["eventStore"] = "application", ["unrelated"] = Secret });
        _ordinaryReplacement = await Allows(nameof(given.operation_queries.Ordinary), new() { ["eventStore"] = "System", ["EventStore"] = "application" });
        _systemReplacement = await Allows(nameof(given.operation_queries.Ordinary), new() { ["eventStore"] = "application", ["EventStore"] = "System" });
    }

    [Fact] void should_match_parameter_names_case_insensitively() => _caseInsensitive.ShouldBeTrue();
    [Fact] void should_accept_a_bound_store_concept() => _concept.ShouldBeTrue();
    [Fact] void should_use_an_explicit_argument_instead_of_the_default() => _override.ShouldBeTrue();
    [Fact] void should_follow_arcs_case_insensitive_replacement_with_an_ordinary_store() => _ordinaryReplacement.ShouldBeTrue();
    [Fact] void should_follow_arcs_case_insensitive_replacement_with_the_system_store() => _systemReplacement.ShouldBeFalse();
}
