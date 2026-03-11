// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Observation.Reactors.Kernel;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Setup.Authentication;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Setup.for_ChronicleInitializer.given;

public class all_dependencies : Specification
{
    protected IStorage _storage;
    protected IEventTypes _eventTypes;
    protected IReactors _reactors;
    protected IProjectionsServiceClient _projectionsServiceClient;
    protected IGrainFactory _grainFactory;
    internal IAuthenticationService _authenticationService;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _eventTypes = Substitute.For<IEventTypes>();
        _reactors = Substitute.For<IReactors>();
        _projectionsServiceClient = Substitute.For<IProjectionsServiceClient>();
        _grainFactory = Substitute.For<IGrainFactory>();
        _authenticationService = Substitute.For<IAuthenticationService>();
    }
}
