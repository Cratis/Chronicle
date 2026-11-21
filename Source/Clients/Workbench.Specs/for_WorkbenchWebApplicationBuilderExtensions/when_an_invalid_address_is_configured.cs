// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Workbench.for_WorkbenchWebApplicationBuilderExtensions;

public class when_an_invalid_address_is_configured : Specification
{
    Exception _exception;

    void Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cratis:Chronicle:Workbench:Address"] = "example.invalid"
        });
        builder.UseCratisChronicleWorkbench();
        using var services = builder.Services.BuildServiceProvider();
        _exception = Catch.Exception(() => _ = services.GetRequiredService<IOptions<ChronicleWorkbenchOptions>>().Value);
    }

    [Fact] void should_reject_the_options_on_start() => _exception.ShouldBeOfExactType<OptionsValidationException>();
    [Fact] void should_explain_the_supported_addresses() => _exception.Message.ShouldContain("Workbench Address must be empty, localhost, or a valid IP address.");
}
