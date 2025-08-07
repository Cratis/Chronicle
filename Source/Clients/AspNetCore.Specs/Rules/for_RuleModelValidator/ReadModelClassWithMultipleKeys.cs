// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.AspNetCore.Rules.for_Rules.for_RulesModelValidator;

public class ReadModelClassWithMultipleKeys
{
    [ReadModelKey]
    public string Id { get; set; }

    [ReadModelKey]
    public string SecondId { get; set; }
}
