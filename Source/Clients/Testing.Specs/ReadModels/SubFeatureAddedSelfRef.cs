// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

[EventType]
public record SubFeatureAddedSelfRef(FeatureNodeId ParentFeatureId, FeatureNodeId FeatureId, string Name);
