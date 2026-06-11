// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

global using Cratis.Arc.Commands.ModelBound;
global using Cratis.Arc.Queries.ModelBound;
global using Cratis.Concepts;
global using Microsoft.AspNetCore.Mvc;

// Arc 20.41.0 added Cratis.Arc.Queries.ModelBound.RouteAttribute, which collides with
// Microsoft.AspNetCore.Mvc.RouteAttribute. These controllers use ASP.NET MVC routing, so
// resolve [Route] to the MVC attribute.
global using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;
