// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_deep_self_referential_children;

[EventType]
public record SubFeatureAdded(Guid ParentFeatureId, string Name);
