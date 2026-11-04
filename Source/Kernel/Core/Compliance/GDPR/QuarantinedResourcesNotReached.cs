// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.GDPR;

/// <summary>
/// Exception that gets thrown when an operation left quarantined event stores or namespaces out, and so did not reach them.
/// </summary>
/// <param name="resources">A description of the event stores and namespaces that were left out.</param>
public class QuarantinedResourcesNotReached(string resources)
    : Exception($"The operation did not reach the quarantined event stores or namespaces {resources}. Repeat it once their name collision is resolved")
{
    /// <summary>
    /// Gets a description of the event stores and namespaces that were left out.
    /// </summary>
    public string Resources { get; } = resources;
}
