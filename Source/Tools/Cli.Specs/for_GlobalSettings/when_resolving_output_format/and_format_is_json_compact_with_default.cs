// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.for_GlobalSettings.when_resolving_output_format;

[Collection(CliSpecsCollection.Name)]
public class and_format_is_json_compact_with_default : Specification
{
    GlobalSettings _settings;
    string _result;

    void Establish() => _settings = new GlobalSettings { Output = OutputFormats.JsonCompact };

    void Because() => _result = _settings.ResolveOutputFormat();

    [Fact] void should_pass_json_compact_through_to_output_formatter() => _result.ShouldEqual(OutputFormats.JsonCompact);
}
