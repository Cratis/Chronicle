// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Legacy exception for a missing projection definition. Chronicle does not throw it; projections use <see cref="IProjectionFor{TReadModel}"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MissingImmediateProjectionForModel"/>.
/// </remarks>
/// <param name="readModelType">Type of read model.</param>
public class MissingImmediateProjectionForModel(Type readModelType)
    : Exception($"Missing projection definition for model of type '{readModelType.FullName}'.");
