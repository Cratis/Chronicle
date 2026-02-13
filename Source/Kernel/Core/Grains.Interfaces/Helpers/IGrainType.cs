// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans;

/// <summary>
/// Represents a way to specify the grain type.
/// </summary>
/// <remarks>
/// This is very useful during testing, where you want to be able wrap the grain type implementation and
/// the convention of IFoo -> Foo will fall over when trying to discover the grain type using the
/// <see cref="GrainExtensions.GetGrainType(IGrain)"/> method.
/// </remarks>
public interface IGrainType
{
    /// <summary>
    /// Gets the <see cref="Type"/> of the grain. The grain type is the interface that the grain implements.
    /// </summary>
    Type GrainType { get; }
}
