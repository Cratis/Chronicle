// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Setup.for_ComplianceEncryptionKeyStorageExtensions;

[PerFactSpecificationLifecycle]
public class when_capturing_the_default_fence_authority : given.a_chronicle_builder
{
    [Fact]
    public void should_use_the_selected_authority_even_without_migration()
    {
        _services.AddSingleton<IEncryptionKeyStorage>(_defaultStorage);
        _services.AddSingleton<IEncryptionKeyFence>(_defaultStorage);
        IEncryptionKeyFence actual = null;
        _builder.WithComplianceEncryptionKeyStorage(OptionsMigratingFromTheDefaultStorage(false), (_, authority) =>
        {
            actual = authority;
            return _dedicatedStorage;
        });
        _services.AddSingleton(Substitute.For<IEncryptionKeyFence>());
        Resolve().ShouldEqual(_dedicatedStorage);
        ReferenceEquals(actual, _defaultStorage).ShouldBeTrue();
    }

    [Fact]
    public void should_keep_the_same_authority_when_composing_with_the_default_store()
    {
        _services.AddSingleton<IEncryptionKeyStorage>(_defaultStorage);
        _services.AddSingleton<IEncryptionKeyFence>(_defaultStorage);
        IEncryptionKeyFence actual = null;
        _builder.WithComplianceEncryptionKeyStorage(OptionsMigratingFromTheDefaultStorage(true), (_, authority) =>
        {
            actual = authority;
            return _dedicatedStorage;
        });
        Resolve().ShouldBeOfExactType<CompositeEncryptionKeyStorage>();
        ReferenceEquals(actual, _defaultStorage).ShouldBeTrue();
    }
}
