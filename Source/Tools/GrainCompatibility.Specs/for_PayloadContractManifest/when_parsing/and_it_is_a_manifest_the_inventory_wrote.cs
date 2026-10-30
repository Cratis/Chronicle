// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadContractManifest.when_parsing;

public class and_it_is_a_manifest_the_inventory_wrote : Specification
{
    const string Manifest = "concept Cratis.Id : string\n"
        + "custom Cratis.Path\n"
        + "enum Cratis.Status : int\n  Active = 1\n"
        + "json Cratis.Payload\n  id: Cratis.Id\n"
        + "oneof Cratis.Result<A,B>\n  case0: A\n"
        + "orleans [Alias(\"An\\\"Alias\\\\Value\")] Cratis.Generated\n  [Id(0)] _value: int?\n";

    string _text;

    void Because()
    {
        var parsed = PayloadContractManifest.Parse(Manifest);
        _text = PayloadContractManifest.Parse(parsed.ToText()).ToText();
    }

    [Fact] void should_preserve_the_manifest() => _text.ShouldEqual(PayloadContractManifest.Parse(Manifest).ToText());
    [Fact] void should_preserve_all_codec_kinds() => PayloadContractManifest.Parse(_text).Types.Values.Select(_ => _.Kind).Distinct().Count().ShouldEqual(6);
    [Fact] void should_preserve_the_escaped_alias() => PayloadContractManifest.Parse(_text).Types["An\"Alias\\Value"].Alias.ShouldEqual("An\"Alias\\Value");
}
