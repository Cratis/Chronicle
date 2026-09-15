// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration;

/// <summary>
/// The exception that is thrown when code is asked for in a language and style its client has no API for.
/// </summary>
/// <param name="language">The language that was asked for.</param>
/// <param name="style">The style that was asked for.</param>
public class ProjectionCodeGenerationNotSupported(ProjectionCodeLanguage language, ProjectionCodeStyle style)
    : Exception($"The {language} client does not offer a {style} projection API.");
