// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Represents an implementation of <see cref="ICapturerManager"/>.
/// </summary>
public class CapturerManager : Grain, ICapturerManager
{
    EventStoreName _eventStoreName = EventStoreName.NotSet;

    /// <inheritdoc/>
    public Task Ensure() => Task.CompletedTask;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _eventStoreName = this.GetPrimaryKeyString();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Register(IEnumerable<CaptureDefinition> definitions) =>
        Task.WhenAll(definitions.Select(RegisterDefinition));

    Task RegisterDefinition(CaptureDefinition definition) =>
        GrainFactory.GetGrain<ICaptureTrigger>(definition.Id, _eventStoreName).Configure(definition);
}
