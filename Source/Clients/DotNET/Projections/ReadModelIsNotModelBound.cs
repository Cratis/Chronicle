// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// The exception that is thrown when a read model is registered as model-bound without carrying any model-bound projection attributes.
/// </summary>
/// <param name="readModelType">The read model type that was registered.</param>
public class ReadModelIsNotModelBound(Type readModelType)
    : Exception($"Read model '{readModelType.FullName}' has no model-bound projection attributes, so there is no projection to register for it. Register a projection for it with a definition callback instead.");
