// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Rules;

namespace Cratis.Chronicle.Rules.for_Rules;

public class RuleWithInvalidDefineStateSignature : IRule
{
    public IProjectionBuilderFor<Rule> Builder { get; private set; }
    public int DefineStateCallCount { get; private set; }

    public void DefineState(IProjectionBuilderFor<Rule> builder, int something)
    {
        Builder = builder;
        DefineStateCallCount++;
    }
}
