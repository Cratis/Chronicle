// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Exception that is thrown when an immediate projection is missing for a model type.
/// </summary>
public class MissingImmediateProjectionForModel : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingImmediateProjectionForModel"/>.
    /// </summary>
    /// <param name="modelType">Type of model.</param>
    public MissingImmediateProjectionForModel(Type modelType) : base($"Missing immediate projection definition for model of type '{modelType.FullName}'. Implement one by implementing the interface IImmediateProjectionFor<{modelType.FullName}>.")
    {
    }
}
