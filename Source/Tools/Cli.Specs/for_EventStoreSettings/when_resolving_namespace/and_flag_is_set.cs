// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.for_EventStoreSettings.when_resolving_namespace;

[Collection(CliSpecsCollection.Name)]
public class and_flag_is_set : given.a_temp_config_directory
{
    const string ExpectedNamespace = "my-namespace";

    EventStoreSettings _settings;
    string _result;

    void Establish() => _settings = new EventStoreSettings { Namespace = ExpectedNamespace };

    void Because() => _result = _settings.ResolveNamespace();

    [Fact] void should_return_the_flag_value() => _result.ShouldEqual(ExpectedNamespace);
}
