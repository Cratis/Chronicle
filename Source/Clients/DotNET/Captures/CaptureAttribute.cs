// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Optional attribute used to adorn classes to configure a capture.
/// The capture will have to implement <see cref="ICapturer"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="CaptureAttribute"/>.
/// </remarks>
/// <param name="id">Optional identifier represented as a GUID string.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class CaptureAttribute(string id = "") : Attribute
{
    /// <summary>
    /// Gets the unique identifier for the capture.
    /// </summary>
    public CaptureId Id { get; } = id switch
    {
        "" => CaptureId.NotSet,
        _ => Guid.Parse(id)
    };
}
