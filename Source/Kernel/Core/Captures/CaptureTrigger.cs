// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Represents an implementation of <see cref="ICaptureTrigger"/>.
/// </summary>
public class CaptureTrigger : Grain, ICaptureTrigger
{
    CaptureDefinition? _definition;
    EventStoreName _eventStoreName = EventStoreName.NotSet;

    /// <inheritdoc/>
    public async Task Configure(CaptureDefinition definition)
    {
        _definition = definition;
        _ = this.GetPrimaryKey(out var keyExtension);
        _eventStoreName = keyExtension ?? EventStoreName.NotSet;

        await GrainFactory.GetGrain<ICapturerGrain>(definition.Id, _eventStoreName).SetDefinition(definition);
    }

    /// <inheritdoc/>
    public Task Trigger()
    {
        if (_definition is null || _eventStoreName == EventStoreName.NotSet)
        {
            return Task.CompletedTask;
        }

        return GrainFactory.GetGrain<ICapturerGrain>(_definition.Id, _eventStoreName).Capture();
    }
}
