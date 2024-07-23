// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Rules;

namespace Cratis.Chronicle.Rules.for_Rules;

public class RuleWithState : IRule
{
    public string FirstStateValue { get; set; }
    public int SecondStateValue { get; set; }
    public ComplexState ComplexState { get; set; }

    public void DefineState(IProjectionBuilderFor<RuleWithState> builder) => builder
        .From<SomeEvent>(_ =>
        {
        });
}
