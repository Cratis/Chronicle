// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Models;

namespace Cratis.Chronicle.AspNetCore.Rules.for_Rules.for_RulesModelValidator;

public class ModelClassWithMultipleKeys
{
    [ModelKey]
    public string Id { get; set; }

    [ModelKey]
    public string SecondId { get; set; }
}
