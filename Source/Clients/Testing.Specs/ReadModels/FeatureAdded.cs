// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event for adding a feature under a module.
/// </summary>
/// <param name="ModuleId">The parent module identifier.</param>
/// <param name="FeatureId">The feature identifier.</param>
/// <param name="Name">Feature name.</param>
[EventType]
public record FeatureAdded(Guid ModuleId, Guid FeatureId, string Name);
