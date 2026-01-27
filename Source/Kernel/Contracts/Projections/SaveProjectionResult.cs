// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the result of saving a projection.
/// </summary>
[ProtoContract]
public class SaveProjectionResult
{
    /// <summary>
    /// Gets or sets the syntax errors, if any.
    /// </summary>
    [ProtoMember(1)]
    public IEnumerable<ProjectionDeclarationSyntaxError> Errors { get; set; } = [];
}
