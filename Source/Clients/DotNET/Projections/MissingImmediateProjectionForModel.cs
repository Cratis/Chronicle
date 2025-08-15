// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Exception that gets thrown when an projection is missing for a model type.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MissingImmediateProjectionForModel"/>.
/// </remarks>
/// <param name="readModelType">Type of read model.</param>
public class MissingImmediateProjectionForModel(Type readModelType)
    : Exception($"Missing projection definition for model of type '{readModelType.FullName}'. Implement one by implementing the interface IImmediateProjectionFor<{readModelType.FullName}>.");
