// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.MongoDB;

/// <summary>
/// Defines a system that can provide types for MongoDB based read models.
/// </summary>
public interface ICanProvideMongoDBReadModelTypes
{
    /// <summary>
    /// Provide the types.
    /// </summary>
    /// <returns>Collection of <see cref="Type"/> that represents read models.</returns>
    IEnumerable<Type> Provide();
}
