// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Chronicle.Reducers.for_ReducerFingerprint.when_fingerprinting;

public class and_upgrading_from_the_legacy_algorithm : Specification
{
    string _legacy;
    string _upgraded;
    string _nextRelease;

    void Establish()
    {
        // Reproduce the legacy algorithm for this synchronous reducer: stored hashes cannot be normalized.
        var type = given.CompiledReducer.Compile(given.CompiledReducer.Simple);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                     .Where(method => !method.IsSpecialName)
                     .OrderBy(Signature, StringComparer.Ordinal))
        {
            hash.AppendData(Encoding.UTF8.GetBytes(Signature(method)));
            hash.AppendData(method.GetMethodBody().GetILAsByteArray());
        }
        _legacy = Convert.ToHexString(hash.GetHashAndReset());
    }

    void Because()
    {
        _upgraded = ReducerFingerprint.Create(given.CompiledReducer.Compile(given.CompiledReducer.Simple));
        _nextRelease = ReducerFingerprint.Create(given.CompiledReducer.Compile(given.CompiledReducer.Simple, version: "2.0.0.0"));
    }

    [Fact] void should_change_the_stored_hash_once_on_upgrade() => _upgraded.ShouldNotEqual(_legacy);
    [Fact] void should_not_change_the_hash_again_for_an_unchanged_release() => _nextRelease.ShouldEqual(_upgraded);

    static string Signature(MethodInfo method) => $"{method.Name}({string.Join(',', method.GetParameters().Select(parameter => parameter.ParameterType.AssemblyQualifiedName))}):{method.ReturnType.AssemblyQualifiedName}";
}
