// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Captures;

/// <summary>
/// Represents a template assignment mapping operation.
/// </summary>
/// <param name="TargetProperty">The target property.</param>
/// <param name="Template">The template.</param>
public record TemplateAssignOperation(string TargetProperty, string Template) : MapOperation;
