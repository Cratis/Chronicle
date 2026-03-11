// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.for_GlobalSettings.when_resolving_output_format;

[Collection(CliSpecsCollection.Name)]
public class and_format_is_explicitly_set : Specification
{
    GlobalSettings _settings;
    string _result;

    void Establish() => _settings = new GlobalSettings { Output = "JSON" };

    void Because() => _result = _settings.ResolveOutputFormat();

    [Fact] void should_return_the_format_lowercased() => _result.ShouldEqual("json");
}
