// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Projections;

/// <summary>
/// Extension methods for converting <see cref="Contracts.Projections.ProjectionDeclarationSyntaxError"/> to <see cref="ProjectionDeclarationSyntaxError"/>.
/// </summary>
public static class ProjectionDeclarationSyntaxErrorConverters
{
    /// <summary>
    /// Converts a <see cref="Contracts.Projections.ProjectionDeclarationSyntaxError"/> to <see cref="ProjectionDeclarationSyntaxError"/>.
    /// </summary>
    /// <param name="error">The contract error to convert.</param>
    /// <returns>The converted API error.</returns>
    public static ProjectionDeclarationSyntaxError ToApi(this Contracts.Projections.ProjectionDeclarationSyntaxError error) =>
        new(error.Message, error.Line, error.Column);

    /// <summary>
    /// Converts a collection of <see cref="Contracts.Projections.ProjectionDeclarationSyntaxError"/> to <see cref="ProjectionDeclarationSyntaxError"/>.
    /// </summary>
    /// <param name="errors">The contract errors to convert.</param>
    /// <returns>The converted API errors.</returns>
    public static IEnumerable<ProjectionDeclarationSyntaxError> ToApi(this IEnumerable<Contracts.Projections.ProjectionDeclarationSyntaxError> errors) =>
        errors.Select(e => e.ToApi());
}
