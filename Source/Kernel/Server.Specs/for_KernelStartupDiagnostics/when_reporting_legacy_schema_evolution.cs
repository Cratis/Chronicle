// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Server.for_KernelStartupDiagnostics;

public class when_reporting_legacy_schema_evolution : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_warn_only_when_the_ignored_list_is_populated(bool configured)
    {
        var logger = Substitute.For<ILogger<Kernel>>();
        logger.IsEnabled(LogLevel.Warning).Returns(true);
#pragma warning disable CS0618 // Exercise the startup diagnostic for legacy configuration.
        var options = new ChronicleOptions { AdditiveEventTypeSchemaEvolutionEventStores = configured ? ["Orders"] : [] };
#pragma warning restore CS0618
        KernelStartupDiagnostics.ReportLegacySchemaEvolution(options, logger);
        logger.ReceivedCalls().Any(_ => _.GetMethodInfo().Name == "Log" && _.GetArguments()[0] is LogLevel.Warning &&
            _.GetArguments()[1] is EventId eventId && eventId.Name == "AdditiveSchemaEvolutionOptInIgnored").ShouldEqual(configured);
    }
}
