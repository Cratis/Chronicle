// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReleaser.when_releasing;

public class and_all_subject_scoped_values_are_null_and_no_subject_resolves : given.a_read_model_releaser
{
    void Establish() => GivenSchemaFor<Composed>();

    async Task Because() => await _releaser.Release(new Composed("name", null));

    [Fact] void should_have_logged_no_warning() => WarningCount().ShouldEqual(0);
}
