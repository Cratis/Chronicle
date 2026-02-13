// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents a collection of parsing errors for a projection declaration.
/// </summary>
[ProtoContract]
public class ProjectionDeclarationParsingErrors
{
    /// <summary>
    /// Gets or sets the collection of syntax errors.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public IEnumerable<ProjectionDeclarationSyntaxError> Errors { get; set; } = [];
}
