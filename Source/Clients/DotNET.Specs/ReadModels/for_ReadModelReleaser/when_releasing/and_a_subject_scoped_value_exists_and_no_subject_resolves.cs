// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReleaser.when_releasing;

public class and_a_subject_scoped_value_exists_and_no_subject_resolves : given.a_read_model_releaser
{
    void Establish() => GivenSchemaFor<Composed>();

    async Task Because() => await _releaser.Release(new Composed("name", "a@b.c"));

    [Fact] void should_have_logged_one_warning() => WarningCount().ShouldEqual(1);
}
