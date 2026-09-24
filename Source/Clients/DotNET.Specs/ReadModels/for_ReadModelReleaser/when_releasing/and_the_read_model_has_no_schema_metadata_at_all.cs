// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReleaser.when_releasing;

/// <summary>
/// Regression: a read model carrying neither compliance nor security schema metadata must still short-circuit
/// entirely - falling back to <see cref="Subject.NotSet"/> only changes what happens once release is known to have
/// something to do; it never turns "nothing to release" into a round trip.
/// </summary>
public class and_the_read_model_has_no_schema_metadata_at_all : given.a_read_model_releaser
{
    Order _instance;
    Order _result;

    void Establish()
    {
        GivenSchemaFor<Order>();
        _instance = new Order(Guid.NewGuid(), "Alice");
    }

    async Task Because() => _result = await _releaser.Release(_instance);

    [Fact] void should_not_have_called_release() => _compliance.DidNotReceive().Release(Arg.Any<Contracts.Compliance.ReleaseRequest>());
    [Fact] void should_hand_back_the_very_same_instance() => ReferenceEquals(_instance, _result).ShouldBeTrue();
}
