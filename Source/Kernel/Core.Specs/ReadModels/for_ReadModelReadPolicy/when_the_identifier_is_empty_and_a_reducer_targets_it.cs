// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReadPolicy;

public class when_the_identifier_is_empty_and_a_reducer_targets_it : given.a_read_policy
{
    void Establish() => _reducers.HasReducerForReadModel(_definition.Identifier).Returns(true);

    async Task Because() => _error = await Catch.Exception(() => ReadModelReadPolicy.EnsureReadable(_grains, "store", _definition));

    [Fact] void should_allow_the_read() => _error.ShouldBeNull();
}
