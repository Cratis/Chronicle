// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_EncryptionKeyCommand;

public class when_parsing_arguments : Specification
{
    EncryptionKeyCommand _verify;
    EncryptionKeyCommand _none;

    void Because()
    {
        _verify = EncryptionKeyCommand.Parse(["--urls", "http://x", "--verify-encryption-keys"]);
        _none = EncryptionKeyCommand.Parse(["--urls", "http://x"]);
    }

    [Fact] void should_recognize_the_verify_flag() => _verify.Verify.ShouldBeTrue();
    [Fact] void should_leave_the_other_arguments_for_the_host() => _verify.RemainingArgs.ShouldEqual(["--urls", "http://x"]);
    [Fact] void should_not_verify_without_the_flag() => _none.Verify.ShouldBeFalse();
    [Fact] void should_keep_every_argument_without_the_flag() => _none.RemainingArgs.ShouldEqual(["--urls", "http://x"]);
}
