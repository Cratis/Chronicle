// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Projections;

namespace Aksio.Cratis.Applications.Rules.for_Rules.for_RulesModelValidator;

public record ModelWithMultipleKeys([ModelKey] string Id, [ModelKey] string SecondId);
