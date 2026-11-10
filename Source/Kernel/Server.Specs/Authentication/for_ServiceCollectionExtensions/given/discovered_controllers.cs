// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Server.Authentication.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions.given;

public class discovered_controllers : chronicle_authentication_services
{
    protected IEnumerable<Type> DiscoverControllers(bool authenticationEnabled)
    {
        // The server hosts MVC through the Workbench API, which adds the controllers; the order matches the host.
        using var provider = BuildServices(
            authenticationEnabled: authenticationEnabled,
            configureServices: services => services.AddControllers()).ServiceProvider;

        var manager = provider.GetRequiredService<ApplicationPartManager>();
        foreach (var assembly in new[] { typeof(AuthorizationController).Assembly, typeof(UnrelatedController).Assembly })
        {
            if (!manager.ApplicationParts.OfType<AssemblyPart>().Any(_ => _.Assembly == assembly))
            {
                manager.ApplicationParts.Add(new AssemblyPart(assembly));
            }
        }

        var feature = new ControllerFeature();
        manager.PopulateFeature(feature);
        return feature.Controllers.Select(_ => _.AsType()).ToList();
    }
}
