// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.AST;

/// <summary>
/// Represents the key directive.
/// </summary>
/// <param name="Property">The key property.</param>
public record KeyDirective(string Property) : CaptureDirective;
