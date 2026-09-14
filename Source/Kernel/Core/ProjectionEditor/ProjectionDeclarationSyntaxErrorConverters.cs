// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProjectionEditor;

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

    /// <summary>
    /// Converts a <see cref="ProjectionDeclarationSyntaxError"/> to its generated contract representation.
    /// </summary>
    /// <param name="error">The error to convert.</param>
    /// <returns>The contract error.</returns>
    /// <remarks>
    /// The generated ProjectionEditor service mirrors this type into its own contract namespace, which is a
    /// different type from the hand-written <see cref="Contracts.Projections.ProjectionDeclarationSyntaxError"/>
    /// the client-facing projections contract still carries.
    /// </remarks>
    public static Contracts.ProjectionEditor.ProjectionDeclarationSyntaxError ToContract(this ProjectionDeclarationSyntaxError error) =>
        new()
        {
            Message = error.Message,
            Line = error.Line,
            Column = error.Column
        };
}
