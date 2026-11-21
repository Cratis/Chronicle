// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given;

public class an_event_sequence_storage_recording_commands : an_event_sequence_storage
{
    protected RecordingCommands _commands = new();

    void Establish() => _interceptors = [_commands];
}
