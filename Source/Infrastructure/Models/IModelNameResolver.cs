// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Models;

/// <summary>
/// Defines a resolver for getting the name of a read model.
/// </summary>
public interface IModelNameResolver
{
    /// <summary>
    /// Get the name of the read model.
    /// </summary>
    /// <param name="readModelType">Type of read model to get for.</param>
    /// <returns>Name of read model.</returns>
    string GetNameFor(Type readModelType);
}
