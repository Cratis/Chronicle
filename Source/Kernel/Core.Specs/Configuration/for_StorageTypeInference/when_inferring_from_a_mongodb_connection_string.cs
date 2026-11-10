// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Configuration.for_StorageTypeInference;

public class when_inferring_from_a_mongodb_connection_string : Specification
{
    string? _plain;
    string? _srv;

    void Because()
    {
        _plain = StorageTypeInference.Infer("mongodb://localhost:27017");
        _srv = StorageTypeInference.Infer("mongodb+srv://cluster.example.net");
    }

    [Fact] void should_infer_mongodb_for_the_plain_scheme() => _plain.ShouldEqual(StorageType.MongoDB);
    [Fact] void should_infer_mongodb_for_the_srv_scheme() => _srv.ShouldEqual(StorageType.MongoDB);
}
