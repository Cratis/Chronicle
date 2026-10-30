// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations.for_PromotionGateMode;

public class when_identifying_modes : Specification
{
    [Theory]
    [InlineData(PromotionGateMode.OnCatchUp, 0)]
    [InlineData(PromotionGateMode.Operator, 1)]
    [InlineData(PromotionGateMode.Deployment, 2)]
    [InlineData(PromotionGateMode.Gated, 3)]
    public void should_preserve_the_mode_value(PromotionGateMode mode, int expected) => ((int)mode).ShouldEqual(expected);
}
