// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Represents an implementation of <see cref="ICapturerGrain"/>.
/// </summary>
public class Capturer : Grain, ICapturerGrain
{
    CaptureDefinition? _definition;

    /// <inheritdoc/>
    public Task SetDefinition(CaptureDefinition definition)
    {
        _definition = definition;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Capture()
    {
        if (_definition is null)
        {
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }
}
