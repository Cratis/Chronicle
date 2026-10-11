// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_a_function_named_member.given;

public record WeekModel([SetFrom<WeekRecorded>, NoAutoMap] int Week);
