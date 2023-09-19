// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Aksio.Cratis.Kernel.Orleans.StateMachines;

public class StateWithAllowedTransitionState : BaseState
{
    public override StateName Name => "State with allowed transition";

    protected override IImmutableList<Type> AllowedTransitions => new[] { typeof(StateThatSupportsTransitioningFrom) }.ToImmutableList();
}
