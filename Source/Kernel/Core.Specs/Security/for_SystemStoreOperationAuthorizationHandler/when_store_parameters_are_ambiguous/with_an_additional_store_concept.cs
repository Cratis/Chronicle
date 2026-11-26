// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler.when_store_parameters_are_ambiguous;

public class with_an_additional_store_concept : given.a_query_operation_policy
{
    readonly List<bool> _allowed = [];

    async Task Because()
    {
        foreach (var enforce in new[] { false, true })
        {
            _enforce = enforce;
            foreach (var destructive in new[] { false, true })
            {
                _enforceDestructive = destructive;
                await CheckDeclarations();
            }
        }
    }

    async Task CheckDeclarations()
    {
        _allowed.Add(await Allows(nameof(given.operation_queries.AnotherStoreType), new() { ["eventStore"] = "application", ["otherStore"] = new EventStoreName("System") }));
        _allowed.Add(await Allows(nameof(given.operation_queries.AnotherStoreConcept), new() { ["eventStore"] = new EventStoreName("application"), ["otherStore"] = new EventStoreName("System") }));
        _allowed.Add(await Allows(nameof(given.operation_queries.AnotherOptionalStore), new() { ["eventStore"] = "application" }));
    }

    [Fact] void should_refuse_every_ambiguous_declaration_in_every_mode() => _allowed.ShouldEachConformTo(_ => !_);
    [Fact] void should_cover_each_declaration_in_all_four_modes() => _allowed.Count.ShouldEqual(12);
    [Fact] void should_not_audit_allow_an_ambiguous_declaration() => _logger.Entries.ShouldBeEmpty();
}
