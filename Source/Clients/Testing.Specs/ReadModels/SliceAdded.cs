// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event for adding a slice under a feature.
/// </summary>
/// <param name="FeatureId">The parent feature identifier.</param>
/// <param name="SliceId">The slice identifier.</param>
/// <param name="Name">Slice name.</param>
[EventType]
public record SliceAdded(Guid FeatureId, Guid SliceId, string Name);
