// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Exception that gets thrown when the web application factory is invalid.
/// </summary>
public class InvalidWebApplicationFactory() : Exception($"{nameof(WebApplicationFactory<object>)} must have a public constructor that only takes {nameof(IChronicleSetupFixture)}, {nameof(Action<IWebHostBuilder>)} and {nameof(ContentRoot)} parameters");
