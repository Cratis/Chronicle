// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.AspNetCore.for_ChronicleAspNetCoreStartupDiagnostics;

public class when_use_cratis_chronicle_was_called : given.startup_diagnostics
{
    void Establish() => _marker.MarkCalled();

    void Because() => Create().Report(_logger);

    [Fact] void should_not_warn() => _logger.Entries.ShouldBeEmpty();
}
