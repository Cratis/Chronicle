// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.for_ChronicleOptionsConfiguration;

public class when_resolving_the_default_event_store : Specification
{
    string? _dotted;
    string? _unresolvable;
    string? _identifierWins;
    string? _longDotted;
    string? _invalidCharacters;
    string? _onlyInvalid;
    ValidateOptionsResult _validation;

    void Because()
    {
        _dotted = ChronicleOptionsConfiguration.ResolveDefaultEventStore(ChronicleOptions.DefaultProgramIdentifier, "My.App");
        _unresolvable = ChronicleOptionsConfiguration.ResolveDefaultEventStore(ChronicleOptions.DefaultProgramIdentifier, null);
        _identifierWins = ChronicleOptionsConfiguration.ResolveDefaultEventStore("billing", "My.App");
        _longDotted = ChronicleOptionsConfiguration.ResolveDefaultEventStore(ChronicleOptions.DefaultProgramIdentifier, "Company.Orders.Api.With.A.Very.Long.Assembly.Name.Indeed");
        _invalidCharacters = ChronicleOptionsConfiguration.ResolveDefaultEventStore("my app/v2:$x?+y", null);
        _onlyInvalid = ChronicleOptionsConfiguration.ResolveDefaultEventStore(ChronicleOptions.DefaultProgramIdentifier, "...");

        var services = new ServiceCollection();
        services.AddOptions<ChronicleClientOptions>().WithDefaultEventStore();
        using var provider = services.BuildServiceProvider();
        _validation = provider.GetRequiredService<IValidateOptions<ChronicleClientOptions>>().Validate(Options.DefaultName, new ChronicleClientOptions());
    }

    [Fact] void should_replace_dots_in_an_assembly_name() => _dotted.ShouldEqual("My-App");
    [Fact] void should_replace_every_forbidden_character_and_collapse_runs() => _invalidCharacters.ShouldEqual("my-app-v2-x-y");
    [Fact] void should_cap_the_length() => (System.Text.Encoding.UTF8.GetByteCount(_longDotted!) <= ChronicleOptionsConfiguration.MaximumDerivedLength).ShouldBeTrue();
    [Fact] void should_not_end_a_capped_name_with_a_separator() => _longDotted!.EndsWith('-').ShouldBeFalse();
    [Fact] void should_resolve_nothing_when_nothing_valid_remains() => _onlyInvalid.ShouldBeNull();
    [Fact] void should_resolve_nothing_without_an_identifier_or_entry_assembly() => _unresolvable.ShouldBeNull();
    [Fact] void should_prefer_the_program_identifier() => _identifierWins.ShouldEqual("billing");
    [Fact] void should_fail_validation_without_an_event_store() => _validation.Failed.ShouldBeTrue();
    [Fact] void should_name_the_configuration_key() => _validation.FailureMessage.ShouldContain("Cratis:Chronicle:EventStore");
}
