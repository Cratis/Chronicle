// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_WriteErrorText;

public class when_describing_a_duplicate_key_error : Specification
{
    const string Message = "E11000 duplicate key error collection: db.readmodel index: unique_email dup key: { Email: \"someone@example.com\" }";
    string _result;
    string _other;

    void Because()
    {
        _result = WriteErrorText.For(ServerErrorCategory.DuplicateKey, Message);
        _other = WriteErrorText.For(ServerErrorCategory.Uncategorized, "some other failure");
    }

    [Fact] void should_name_the_index() => _result.ShouldEqual("duplicate key in index unique_email");
    [Fact] void should_not_contain_the_key_value() => _result.ShouldNotContain("someone@example.com");
    [Fact] void should_keep_other_messages() => _other.ShouldEqual("some other failure");
}
