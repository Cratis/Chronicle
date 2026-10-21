// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyScopeStrategies;

public class when_constructing_with_the_original_signature : Specification
{
    ConcurrencyScopeStrategies _strategies;

    void Because() => _strategies = (ConcurrencyScopeStrategies)typeof(ConcurrencyScopeStrategies)
        .GetConstructor([typeof(ConcurrencyOptions), typeof(IServiceProvider)])!
        .Invoke([new ConcurrencyOptions(), Substitute.For<IServiceProvider>()]);

    [Fact] void should_retain_the_original_clr_signature() => _strategies.ShouldNotBeNull();
    [Fact] void should_use_the_null_logger() => _strategies.Logger.ShouldEqual(NullLogger<EventSequence>.Instance);
}
