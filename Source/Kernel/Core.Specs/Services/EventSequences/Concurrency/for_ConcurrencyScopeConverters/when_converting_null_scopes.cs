// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.EventSequences.Concurrency.for_ConcurrencyScopeConverters;

public class when_converting_null_scopes : Specification
{
    Concepts.EventSequences.Concurrency.ConcurrencyScopes _result;

    void Because()
    {
        IDictionary<string, Contracts.EventSequences.Concurrency.ConcurrencyScope> scopes = null!;
        _result = scopes.ToChronicle();
    }

    [Fact] void should_return_a_result() => _result.ShouldNotBeNull();
    [Fact] void should_have_no_scopes() => _result.Scopes.ShouldBeEmpty();
}
