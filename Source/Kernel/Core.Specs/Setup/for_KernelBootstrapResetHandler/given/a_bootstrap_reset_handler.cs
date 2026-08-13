// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation.Reactors.Kernel;
using Cratis.Chronicle.Setup.Authentication;

namespace Cratis.Chronicle.Setup.for_KernelBootstrapResetHandler.given;

public class a_bootstrap_reset_handler : Specification
{
    KernelBootstrapResetHandler _handler = null!;
    TestAuthenticationService _authenticationService = null!;
    protected IGrainFactory _grainFactory = null!;
    protected IEventTypes _eventTypes = null!;
    protected IReactors _reactors = null!;
    protected INamespaces _systemNamespaces = null!;

    protected int EnsureDefaultAdminUserCount => _authenticationService.EnsureDefaultAdminUserCount;

    protected int EnsureBootstrapClientsCount => _authenticationService.EnsureBootstrapClientsCount;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _eventTypes = Substitute.For<IEventTypes>();
        _reactors = Substitute.For<IReactors>();
        _authenticationService = new TestAuthenticationService();
        _systemNamespaces = Substitute.For<INamespaces>();

        _grainFactory.GetGrain<INamespaces>(EventStoreName.System).Returns(_systemNamespaces);
        _systemNamespaces.EnsureDefault().Returns(Task.CompletedTask);
        _eventTypes.DiscoverAndRegister(EventStoreName.System).Returns(Task.CompletedTask);
        _reactors.DiscoverAndRegister(EventStoreName.System, EventStoreNamespaceName.Default).Returns(Task.CompletedTask);

        _handler = new(_grainFactory, _eventTypes, _reactors, _authenticationService);
    }

    protected Task Bootstrap() => _handler.Bootstrap();

    class TestAuthenticationService : IAuthenticationService
    {
        public int EnsureDefaultAdminUserCount { get; private set; }

        public int EnsureBootstrapClientsCount { get; private set; }

        public Task<Storage.Security.User?> AuthenticateUser(
            Concepts.Security.Username username,
            Concepts.Security.Password password) => Task.FromResult<Storage.Security.User?>(null);

        public Task EnsureDefaultAdminUser()
        {
            EnsureDefaultAdminUserCount++;
            return Task.CompletedTask;
        }

        public Task EnsureBootstrapClients()
        {
            EnsureBootstrapClientsCount++;
            return Task.CompletedTask;
        }
#if DEVELOPMENT
        public Task EnsureDefaultClientCredentials() => Task.CompletedTask;
#endif
    }
}
