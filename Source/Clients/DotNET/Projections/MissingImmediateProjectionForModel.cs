// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Projections;

/// <summary>
/// Exception that gets thrown when an immediate projection is missing for a model type.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MissingImmediateProjectionForModel"/>.
/// </remarks>
/// <param name="modelType">Type of model.</param>
public class MissingImmediateProjectionForModel(Type modelType)
    : Exception($"Missing immediate projection definition for model of type '{modelType.FullName}'. Implement one by implementing the interface IImmediateProjectionFor<{modelType.FullName}>.");
