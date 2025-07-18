// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Operations.given;

public class a_new_event_source_operations : Specification
{
    protected EventSourceOperations _operations;

    void Establish() => _operations = new();
}
