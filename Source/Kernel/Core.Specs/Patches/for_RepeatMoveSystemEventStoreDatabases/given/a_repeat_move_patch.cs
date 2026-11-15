// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Patches;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Core.Specs.Patches.for_RepeatMoveSystemEventStoreDatabases.given;

public class a_repeat_move_patch : Specification
{
    protected RepeatMoveSystemEventStoreDatabases _patch;
    protected IStorage _storage;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _patch = new(_storage, NullLogger<RepeatMoveSystemEventStoreDatabases>.Instance);
    }
}
