// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.AspNetCore.for_ChronicleAspNetCoreStartupDiagnostics;

public class when_use_cratis_chronicle_was_not_called : given.startup_diagnostics
{
    void Because() => Create().Report(_logger);

    [Fact] void should_warn_about_the_missing_call() => _logger.Entries.ShouldContain(e => e.Message.Contains("UseCratisChronicle() was not called"));
}
