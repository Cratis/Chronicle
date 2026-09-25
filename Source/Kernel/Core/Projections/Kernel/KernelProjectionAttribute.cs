// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Kernel;

/// <summary>
/// Attribute used to adorn a class to tell Chronicle that the class declares a system projection.
/// </summary>
/// <param name="id">The identifier of the projection, without the "$system." prefix.</param>
/// <remarks>
/// The prefix is added rather than written, so a declaration cannot accidentally register a system projection
/// under a name that does not look like one - or a client-shaped name under the system owner.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class KernelProjectionAttribute(string id) : Attribute
{
    /// <summary>
    /// Gets the identifier of the projection, without the "$system." prefix.
    /// </summary>
    public string Id { get; } = id;
}
