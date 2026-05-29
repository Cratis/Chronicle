// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Defines a type that can declaratively define a capture.
/// </summary>
public interface ICapturer : ICapture
{
    /// <summary>
    /// Defines the capture.
    /// </summary>
    /// <param name="builder">The <see cref="ICaptureBuilder"/> used to build the capture definition.</param>
    void Define(ICaptureBuilder builder);
}
