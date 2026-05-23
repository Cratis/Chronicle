// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Nested record populated from <see cref="ModuleOpened"/> via property-level <see cref="SetFromAttribute{TEvent}"/>.
/// </summary>
/// <param name="Name">Customer name.</param>
/// <param name="Email">Customer email.</param>
public record ModuleCustomerInfo(
    [SetFrom<ModuleOpened>(nameof(ModuleOpened.CustomerName))]
    string Name,
    [SetFrom<ModuleOpened>(nameof(ModuleOpened.CustomerEmail))]
    string Email);
