// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model used to verify that an unresolved root join cannot resurrect a removed instance.
/// </summary>
/// <param name="Id">Widget identifier.</param>
/// <param name="CustomerId">Customer identifier used by the root join.</param>
/// <param name="CustomerName">Customer name supplied by the root join.</param>
[Passive]
[FromEvent<RemovableJoinedWidgetCreated>]
[RemovedWith<RemovableWidgetDeleted>]
public sealed record RemovableJoinedWidget(
    [Key] Guid Id,

    [SetFrom<RemovableJoinedWidgetCreated>]
    JoinCustomerId CustomerId,

    [Join<JoinCustomerRegistered>(on: nameof(CustomerId), eventPropertyName: nameof(JoinCustomerRegistered.Name))]
    string CustomerName);
