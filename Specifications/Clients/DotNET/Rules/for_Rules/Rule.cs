// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Rules.for_Rules;

public class Rule : IRule
{
    public RuleId Identifier => "06185a2b-b024-4f31-aea9-0f7f11f99299";

    public IProjectionBuilderFor<Rule> Builder { get; private set; }
    public int DefineStateCallCount { get; private set; }

    public void DefineState(IProjectionBuilderFor<Rule> builder)
    {
        Builder = builder;
        DefineStateCallCount++;
    }
}
