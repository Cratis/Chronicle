// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// The exception that is thrown when the kernel accepted the projections a registrant registered, and then could not apply them.
/// </summary>
/// <param name="registrant">The registrant the registration was made as.</param>
/// <param name="reason">Why the kernel could not apply it.</param>
public class ProjectionRegistrationNotApplied(string registrant, string reason) : Exception($"The kernel accepted the projections registered by '{registrant}' but could not apply them: {reason}")
{
    /// <summary>
    /// Gets the registrant the registration was made as.
    /// </summary>
    public string Registrant { get; } = registrant;
}
