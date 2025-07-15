// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Concurrency;

namespace Cratis.Chronicle.Concepts.EventSequences.Concurrency.for_ConcurrencyScope.when_checking_should_be_validated;

public class and_scope_is_None : Specification
{
    [Fact] void should_not_validate() => ConcurrencyScope.None.ShouldBeValidated.ShouldBeFalse();
}
