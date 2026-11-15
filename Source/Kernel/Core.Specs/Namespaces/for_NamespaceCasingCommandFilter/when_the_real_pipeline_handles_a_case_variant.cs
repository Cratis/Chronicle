// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Setup;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Namespaces.for_NamespaceCasingCommandFilter;

public class when_the_real_pipeline_handles_a_case_variant : Specification
{
    ServiceProvider _provider;
    INamespaceRemover _remover;
    CommandResult _result;

    void Establish()
    {
        var casing = Substitute.For<INamespaceCasing>();
        casing.Canonicalize("Studio", "cratis").Returns((EventStoreNamespaceName)"Cratis");
        _remover = Substitute.For<INamespaceRemover>();
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.Configure<ArcOptions>(_ => { });
        services.AddCratisArcCore();
        services.AddKernelSideCommandExtensionPointsForKernelCommands();
        services.AddSingleton(casing);
        var storage = Substitute.For<Cratis.Chronicle.Storage.IStorage>();
        storage.HasEventStore("Studio").Returns(true);
        services.AddSingleton(storage);
        services.AddSingleton(_remover);
        _provider = services.BuildServiceProvider();
    }

    async Task Because()
    {
        using var execution = KernelCommandExecution.Begin();
        _result = await _provider.GetRequiredService<ICommandPipeline>().Execute(new RemoveNamespace("Studio", "cratis"));
    }

    [Fact]
    void should_succeed()
    {
        _result.ExceptionMessages.ShouldBeEmpty();
        _result.ValidationResults.ShouldBeEmpty();
        _result.IsAuthorized.ShouldBeTrue();
    }
    [Fact] async Task should_deliver_the_canonical_name_to_the_handler() => await _remover.Received(1).Remove("Studio", "Cratis");

    void Destroy() => _provider.Dispose();
}
