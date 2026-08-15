// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences.Concurrency.for_ConcurrencyScope.when_checking_should_be_validated;

/// <summary>
/// The whole point of the before-first expectation is that the kernel runs a check for it. If it were sorted with
/// the scopes that carry nothing to compare against, the first append into a scope would still go unchecked and the
/// sentinel would buy nothing.
/// </summary>
public class and_scope_expects_no_matching_event : Specification
{
    ConcurrencyScope _scope;

    void Establish() => _scope = new ConcurrencyScope(Events.EventSequenceNumber.BeforeFirst, true, null, null, null, null);

    [Fact] void should_validate() => _scope.ShouldBeValidated.ShouldBeTrue();
}
