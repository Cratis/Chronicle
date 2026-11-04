// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintKeyedEncoder.when_encoding;

public class and_the_scope_is_unscoped : Specification
{
    readonly byte[] _key = Encoding.ASCII.GetBytes("slice-1-reference-key");
    UniqueConstraintScopeToken _fromNull;
    UniqueConstraintScopeToken _fromNullAgain;
    UniqueConstraintScopeToken _fromEmpty;
    UniqueConstraintScopeToken _static;

    void Because()
    {
        var encoder = new UniqueConstraintKeyedEncoder(_key);
        _fromNull = encoder.EncodeScope(null);
        _fromNullAgain = encoder.EncodeScope(null);
        _fromEmpty = encoder.EncodeScope(new ResolvedConstraintScope());
        _static = UniqueConstraintScopeToken.Encode(_key, null);
    }

    [Fact] void should_equal_the_static_token_for_no_scope() => _fromNull.ShouldEqual(_static);
    [Fact] void should_equal_the_static_token_for_an_unnarrowed_scope() => _fromEmpty.ShouldEqual(_static);
    [Fact] void should_return_the_cached_instance() => ReferenceEquals(_fromNullAgain, _fromNull).ShouldBeTrue();
}
