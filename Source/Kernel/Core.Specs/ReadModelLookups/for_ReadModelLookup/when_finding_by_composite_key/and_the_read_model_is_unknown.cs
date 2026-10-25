// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.ReadModelLookups.for_ReadModelLookup.when_finding_by_composite_key;

public class and_the_read_model_is_unknown : given.a_composite_key_read
{
    Exception _exception;
    void Establish() => _readModel.GetDefinition().Returns((Concepts.ReadModels.ReadModelDefinition)null!);
    async Task Because() => _exception = await Catch.Exception(Read);
    [Fact] void should_preserve_the_read_model_not_found_error() => _exception.ShouldBeOfExactType<ReadModelNotFound>();
    [Fact] Task should_not_look_up_the_instance() => DidNotLookUp();
}
