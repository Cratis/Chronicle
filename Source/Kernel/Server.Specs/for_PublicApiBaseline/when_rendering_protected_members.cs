// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_PublicApiBaseline;

/// <summary>
/// Only externally usable inheritance members belong to the public API.
/// </summary>
public class when_rendering_protected_members : Specification
{
    string _snapshot = string.Empty;
    string _sealed = string.Empty;
    string _internal = string.Empty;

    void Because()
    {
        _snapshot = given.public_api_snapshot.Render([typeof(given.SampleExtensibleContract)]);
        _sealed = given.public_api_snapshot.Render([typeof(given.SampleSealedContract)]);
        _internal = given.public_api_snapshot.Render([typeof(given.a_sample_message)]);
    }

    [Fact] void should_include_record_copy_constructors() => _snapshot.ShouldContain("method protected constructor");
    [Fact] void should_include_record_equality_contracts() => _snapshot.ShouldContain("::EqualityContract() protected");
    [Fact] void should_include_record_print_members() => _snapshot.ShouldContain("::PrintMembers(");
    [Fact] void should_include_protected_constants() => _snapshot.ShouldContain("::Limit protected static const System.Int32 = 7");
    [Fact] void should_include_protected_internal_methods() => _snapshot.ShouldContain("method protected internal virtual System.String! ");
    [Fact] void should_include_protected_setters() => _snapshot.ShouldContain("::Value set protected calling=");
    [Fact] void should_include_public_getters() => _snapshot.ShouldContain("::Value get public calling=");
    [Fact] void should_exclude_internal_members() => _snapshot.ShouldNotContain("::InternalMember");
    [Fact] void should_exclude_private_protected_members() => _snapshot.ShouldNotContain("::PrivateProtectedMember");
    [Fact] void should_exclude_private_members() => _snapshot.ShouldNotContain("::Hidden");
    [Fact] void should_exclude_protected_members_of_sealed_types() => _sealed.ShouldNotContain("protected");
    [Fact] void should_exclude_protected_members_of_internal_types() => _internal.ShouldNotContain("protected");
}
