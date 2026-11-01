// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Supplies immutable type descriptors without retaining a reducer handler or activated instance.
/// </summary>
internal interface IReducerDescriptorsSource
{
    /// <summary>
    /// Discovers the descriptors or adopts an equivalent catalog.
    /// </summary>
    /// <param name="discover">The normal descriptor discovery.</param>
    /// <returns>The immutable type descriptors.</returns>
    IReadOnlyList<ReducerDescriptor> Get(Func<IReadOnlyList<ReducerDescriptor>> discover);
}
