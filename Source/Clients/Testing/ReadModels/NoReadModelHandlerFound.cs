// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// The exception that is thrown when no reducer or projection is found for a given read model type.
/// </summary>
/// <param name="readModelType">The read model type for which no handler was found.</param>
public class NoReadModelHandlerFound(Type readModelType)
    : Exception($"No reducer or projection found for read model type '{readModelType.FullName}'. Ensure that either an IReducerFor<{readModelType.Name}>, IProjectionFor<{readModelType.Name}>, or a model-bound projection exists in the discovered assemblies.");
