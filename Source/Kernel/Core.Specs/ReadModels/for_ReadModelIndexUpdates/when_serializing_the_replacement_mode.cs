// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using ProtoBuf;

namespace Cratis.Chronicle.ReadModels.for_ReadModelIndexUpdates;

public class when_serializing_the_replacement_mode : Specification
{
    UpdateDefinitionRequest _round_tripped;

    void Because() => _round_tripped = Serializer.DeepClone(new UpdateDefinitionRequest
    {
        EventStore = "store",
        ReadModel = new() { Type = new() { Identifier = "model", Generation = 1 } },
        ReplaceIndexes = true
    });

    [Fact] void should_keep_the_complete_replacement_flag() => _round_tripped.ReplaceIndexes.ShouldBeTrue();
    [Fact] void should_keep_the_legacy_default() => new UpdateDefinitionRequest().ReplaceIndexes.ShouldBeFalse();
}
