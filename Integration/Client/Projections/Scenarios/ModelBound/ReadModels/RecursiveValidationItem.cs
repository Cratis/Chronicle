// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.Events;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.ReadModels;

[FromEvent<RecursiveValidationConfiguredOnCommand>]
[FromEvent<RecursiveValidationUpdatedOnCommand>]
[ClearWith<RecursiveValidationRemovedFromCommand>]
public record RecursiveValidationItem(
    [SetFrom<RecursiveValidationConfiguredOnCommand>(nameof(RecursiveValidationConfiguredOnCommand.Rules))]
    [SetFrom<RecursiveValidationUpdatedOnCommand>(nameof(RecursiveValidationUpdatedOnCommand.NewRules))]
    string Rules);
