// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookDetails;

public class when_observing_with_custom_headers : given.stored_headers
{
    readonly List<WebhookDetails> _results = [];

    void Because()
    {
        using var subscription = WebhookDetails.ObserveWebhooks("store", _storage).Subscribe(items => _results.Add(items.Single()));
        _source.OnNext([_definition]);
        _headers["x-first"] = "synthetic-replacement";
        _headers.Remove("x-second");
        _headers["x-empty"] = "synthetic-new";
        _headers["x-whitespace"] = string.Empty;
        _source.OnNext([_definition]);
    }

    [Fact] void should_emit_both_populated_updates() => _results.Count.ShouldEqual(2);
    [Fact] void should_redact_every_emission() => _results.SelectMany(_ => _.Headers.Values).All(_ => _.Length == 0).ShouldBeTrue();
    [Fact] void should_exclude_all_values_from_json() => JsonSerializer.Serialize(_results).ShouldNotContain("synthetic-");
    [Fact] void should_keep_the_initial_names_snapshot() => _results[0].HeaderValuesSet.Keys.ShouldContainOnly("x-first", "x-second", "x-empty", "x-whitespace");
    [Fact] void should_keep_the_initial_empty_flag() => _results[0].HeaderValuesSet["x-empty"].ShouldBeFalse();
    [Fact] void should_report_replacement_as_set() => _results[1].HeaderValuesSet["x-first"].ShouldBeTrue();
    [Fact] void should_update_the_empty_flag() => _results[1].HeaderValuesSet["x-empty"].ShouldBeTrue();
    [Fact] void should_update_the_whitespace_flag() => _results[1].HeaderValuesSet["x-whitespace"].ShouldBeFalse();
    [Fact] void should_remove_names_from_legacy_headers() => _results[1].Headers.Keys.ShouldContainOnly("x-first", "x-empty", "x-whitespace");
    [Fact] void should_remove_names_from_metadata() => _results[1].HeaderValuesSet.Keys.ShouldContainOnly("x-first", "x-empty", "x-whitespace");
}
