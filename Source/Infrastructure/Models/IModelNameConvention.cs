// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Models;

/// <summary>
/// Defines a convention for getting the name of a model.
/// </summary>
public interface IModelNameConvention
{
    /// <summary>
    /// Get the name of the model.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to get for.</param>
    /// <returns>Name of the model.</returns>
    string GetNameFor(Type type);
}
