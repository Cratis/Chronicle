// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_EncryptionKeyCommandRunner;

public class when_migrating_and_the_layout_is_unusable : given.a_migrator
{
    int? _exit;

    void Establish() => Returns(Report(layoutUnusable: true, complete: false));

    async Task Because() => _exit = await Migrate();

    [Fact] void should_report_the_layout() => _exit.ShouldEqual(4);
}
