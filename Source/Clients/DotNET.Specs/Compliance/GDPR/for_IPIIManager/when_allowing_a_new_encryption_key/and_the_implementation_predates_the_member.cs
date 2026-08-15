// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.GDPR.for_IPIIManager.when_allowing_a_new_encryption_key;

/// <summary>
/// AllowNewEncryptionKeyFor was added to a shipped interface, so it carries a default implementation - an
/// implementation written before it keeps compiling, which is what keeps this release additive.
/// </summary>
/// <remarks>
/// The default throws rather than doing nothing. An authorization that silently did not happen reads as a subject
/// who can be protected again, right up until the append that proves they cannot - and the failure would surface
/// somewhere else entirely, long after the call that was supposed to perform it. Naming the type and the member
/// puts it where it happened.
/// </remarks>
public class and_the_implementation_predates_the_member : given.two_implementations
{
    IPIIManager _manager;
    Exception _error;
    Exception _erasureError;

    void Establish() => _manager = new predates_the_member();

    async Task Because()
    {
        _error = await Catch.Exception(() => _manager.AllowNewEncryptionKeyFor(Identifier));
        _erasureError = await Catch.Exception(() => _manager.DeleteEncryptionKeyFor(Identifier));
    }

    [Fact] void should_fail_rather_than_silently_doing_nothing() => _error.ShouldBeOfExactType<NotSupportedException>();
    [Fact] void should_name_the_implementation() => _error.Message.ShouldContain(typeof(predates_the_member).FullName!);
    [Fact] void should_name_the_member() => _error.Message.ShouldContain(nameof(IPIIManager.AllowNewEncryptionKeyFor));
    [Fact] void should_still_support_erasing() => _erasureError.ShouldBeNull();
}
