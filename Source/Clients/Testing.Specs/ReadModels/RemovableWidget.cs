// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Root read model created from <see cref="RemovableWidgetCreated"/> and removed entirely via the
/// class-level <see cref="RemovedWithAttribute{TEvent}"/> when <see cref="RemovableWidgetDeleted"/> is observed.
/// </summary>
/// <param name="Id">Widget identifier (the event source id).</param>
/// <param name="Name">Widget name.</param>
[Passive]
[FromEvent<RemovableWidgetCreated>]
[RemovedWith<RemovableWidgetDeleted>]
public sealed record RemovableWidget(
    [Key] Guid Id,
    string Name);
