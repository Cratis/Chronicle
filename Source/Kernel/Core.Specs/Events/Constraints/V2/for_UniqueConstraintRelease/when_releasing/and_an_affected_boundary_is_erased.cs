// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintRelease.when_releasing;

public class and_an_affected_boundary_is_erased : given.a_factory
{
    Dictionary<string, ReleasedUniqueConstraintComponents> _results;

    void Establish() => SetContent("""{"name":"ciphertext","number":"opaque integer","identifier":"opaque guid","day":"opaque date","profile":"opaque object","names":"opaque array","nameSuffix":"readable"}""");

    async Task Because()
    {
        _results = [];
        foreach (var (property, boundary) in new (string, string)[]
        {
            ("number", "number"), ("identifier", "identifier"), ("day", "day"),
            ("profile.name", "PROFILE"), ("names", "names"), ("names", "names[0]"),
            ("profile", "profile.name"), ("nameSuffix", "name")
        })
        {
            _definition = new("event", [property]);
            _statuses.Clear();
            _statuses[boundary] = ProtectedValueReleaseStatus.Erased;
            _results[property + ":" + boundary] = await Create();
        }
    }

    [Fact] void should_skip_erased_numeric_pii() => _results["number:number"].ShouldSkipClaimOrRemoval.ShouldBeTrue();
    [Fact] void should_skip_erased_encrypted_guids() => _results["identifier:identifier"].ShouldSkipClaimOrRemoval.ShouldBeTrue();
    [Fact] void should_skip_erased_encrypted_dates() => _results["day:day"].ShouldSkipClaimOrRemoval.ShouldBeTrue();
    [Fact] void should_propagate_parent_object_erasure_to_selected_children() => _results["profile.name:PROFILE"].ShouldSkipClaimOrRemoval.ShouldBeTrue();
    [Fact] void should_skip_an_erased_array_boundary() => _results["names:names"].ShouldSkipClaimOrRemoval.ShouldBeTrue();
    [Fact] void should_skip_an_array_with_an_erased_element() => _results["names:names[0]"].ShouldSkipClaimOrRemoval.ShouldBeTrue();
    [Fact] void should_skip_an_object_with_an_erased_child() => _results["profile:profile.name"].ShouldSkipClaimOrRemoval.ShouldBeTrue();
    [Fact] void should_not_confuse_a_name_prefix_with_a_boundary() => _results["nameSuffix:name"].ShouldSkipClaimOrRemoval.ShouldBeFalse();
    [Fact] void should_not_return_null_or_empty_substitutes_for_erasure() => _results["number:number"].Components.Count.ShouldEqual(0);
    [Fact] void should_not_mutate_stored_content() => CurrentContent().ShouldEqual(_original);
}
