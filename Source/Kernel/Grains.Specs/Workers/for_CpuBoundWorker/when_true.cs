// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Workers.for_LimitedConcurrencyLevelTaskScheduler;

public class when_true : Specification
{
    [Fact] void should_be_true() => true.ShouldBeTrue();
}
