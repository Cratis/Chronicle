// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_StorageTypeInference;

public class when_inferring_from_an_unrecognizable_connection_string : Specification
{
    string? _unrecognizable;
    string? _empty;

    void Because()
    {
        _unrecognizable = StorageTypeInference.Infer("something-odd");
        _empty = StorageTypeInference.Infer(string.Empty);
    }

    [Fact] void should_not_infer_a_type_for_unknown_details() => _unrecognizable.ShouldBeNull();
    [Fact] void should_not_infer_a_type_for_empty_details() => _empty.ShouldBeNull();
}
