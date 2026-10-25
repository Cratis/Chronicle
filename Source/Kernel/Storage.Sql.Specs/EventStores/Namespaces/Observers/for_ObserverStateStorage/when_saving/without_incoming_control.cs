// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_saving;

public class without_incoming_control : given.an_observer_state_storage
{
    ObserverState _saved;

    async Task Establish()
    {
        await using var context = CreateContext();
        context.Observers.Add(new() { Id = "observer", TypeSetControl = "{}" });
        await context.SaveChangesAsync();
    }

    async Task Because()
    {
        await _storage.Save(new() { Identifier = "observer", NextEventSequenceNumber = new(123) }, Concepts.EventSequences.EventSequenceId.Log);
        await using var context = CreateContext();
        _saved = await context.Observers.SingleAsync();
    }

    [Fact] void should_preserve_control_without_parsing_its_shape() => _saved.TypeSetControl.ShouldEqual("{}");
    [Fact] void should_save_progress() => _saved.NextEventSequenceNumber.ShouldEqual(123UL);
}
