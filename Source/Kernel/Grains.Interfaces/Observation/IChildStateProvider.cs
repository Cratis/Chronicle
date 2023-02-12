// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Defines a child state provider.
/// </summary>
/// <typeparam name="T">Type of the State that will be provided.</typeparam>
public interface IChildStateProvider<out T>
{
    /// <summary>
    /// Gets the current state for the child.
    /// </summary>
    /// <returns>An instance of T as the State.</returns>
    T GetState();
}