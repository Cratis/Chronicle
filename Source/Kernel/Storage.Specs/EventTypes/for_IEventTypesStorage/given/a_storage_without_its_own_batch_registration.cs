// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.EventTypes.for_IEventTypesStorage.given;

public class a_storage_without_its_own_batch_registration : Specification
{
    protected IEventTypesStorage _inner;
    protected IEventTypesStorage _subject;

    void Establish()
    {
        _inner = Substitute.For<IEventTypesStorage>();
        _subject = new DelegatingEventTypesStorage(_inner);
    }

    protected static EventTypeToRegister EventTypeToRegisterFor(
        string eventTypeId,
        EventTypeMigrationDefinition[] migrations,
        params uint[] generations) =>
        new(
            new EventTypeDefinition(
                eventTypeId,
                EventTypeOwner.Client,
                false,
                generations.Select(_ => new EventTypeGenerationDefinition(_, new JsonSchema())).ToArray(),
                migrations),
            EventTypeSource.Code);
}
