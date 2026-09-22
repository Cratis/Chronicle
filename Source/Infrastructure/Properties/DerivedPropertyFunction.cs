// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties;

/// <summary>
/// Represents a named, argument-less function that can appear as the terminal segment of a <see cref="PropertyPath"/>
/// to derive a value from whatever the rest of the path resolved to, instead of reading a real reflectable property.
/// </summary>
/// <param name="Name">
/// The name of the function - identical to the C# extension method name used in the accessor expression
/// (for example <c language="csharp">ISOWeek</c> for <c language="csharp">c => c.Occurred.ISOWeek()</c>) and to
/// how it is rendered as the terminal segment of a <see cref="PropertyPath"/> (<c language="csharp">Occurred.ISOWeek()</c>).
/// </param>
/// <param name="Evaluate">Computes the derived value from the value the path resolved to immediately before this segment.</param>
public record DerivedPropertyFunction(string Name, Func<object, object?> Evaluate);
