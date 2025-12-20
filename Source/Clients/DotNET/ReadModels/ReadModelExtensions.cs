// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Extension methods for working with read models.
/// </summary>
public static class ReadModelExtensions
{
    /// <summary>
    /// Check if a read model type is marked as passive.
    /// </summary>
    /// <param name="readModelType">The read model type to check.</param>
    /// <returns>True if the read model is passive, false otherwise.</returns>
    public static bool IsPassive(this Type readModelType) => Attribute.IsDefined(readModelType, typeof(PassiveAttribute));
}
