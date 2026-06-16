// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Integration.for_Reactors;

public class UniqueReactorSideEffectConstraint : IConstraint
{
    public void Define(IConstraintBuilder builder) => builder
        .Unique(_ => _.On<UniqueReactorSideEffect>(e => e.Value)
            .WithMessage("Reactor side-effect value must be unique"));
}
