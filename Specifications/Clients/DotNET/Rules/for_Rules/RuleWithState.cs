// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Rules.for_Rules;

public class RuleWithState : IRule
{
    public RuleId Identifier => "06185a2b-b024-4f31-aea9-0f7f11f99299";

    public string FirstStateValue { get; set; }
    public int SecondStateValue { get; set; }
    public ComplexState ComplexState { get; set; }

    public void DefineState(IProjectionBuilderFor<RuleWithState> builder) => builder
        .From<SomeEvent>(_ =>
        {
        });
}
