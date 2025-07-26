// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.AspNetCore.Rules.for_Rules.for_RulesModelValidator;

public record ModelWithMultipleKeys([ReadModelKey] string Id, [ReadModelKey] string SecondId);
