// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.Events;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.ReadModels;

[FromEvent<CommandSetOnSliceItem>]
[FromEvent<SliceItemCommandRenamed>]
[ClearWith<CommandClearedOnSliceItem>]
public record CommandItem(
    [SetFrom<SliceItemCommandRenamed>(nameof(SliceItemCommandRenamed.NewName))]
    string Name,
    string Schema);
