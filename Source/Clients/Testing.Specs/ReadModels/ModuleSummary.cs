// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test read model that exercises a single event type registered through three mechanisms simultaneously —
/// class-level <see cref="FromEventAttribute{TEvent}"/>, property-level <see cref="SetFromContextAttribute{TEvent}"/>,
/// and a <see cref="NestedAttribute"/> child whose properties use <see cref="SetFromAttribute{TEvent}"/>.
/// </summary>
/// <param name="Id">Identifier.</param>
/// <param name="Name">Module name — auto-mapped from <see cref="ModuleOpened.Name"/>.</param>
/// <param name="Customer">Nested customer details populated from the same <see cref="ModuleOpened"/> event.</param>
/// <param name="OpenedAt">When the module was opened — captured from <see cref="EventContext.Occurred"/> for the same event.</param>
[Passive]
[FromEvent<ModuleOpened>]
public record ModuleSummary(
    Guid Id,
    string Name,

    [Nested]
    ModuleCustomerInfo? Customer,

    [SetFromContext<ModuleOpened>(nameof(EventContext.Occurred))]
    DateTimeOffset? OpenedAt);
