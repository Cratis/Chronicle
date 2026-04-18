// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// An <see cref="IServiceCollection"/> implementation that captures the service descriptors
/// added by test fixtures without registering them into a real DI container.
/// </summary>
internal class CapturingServiceCollection : List<ServiceDescriptor>, IServiceCollection;
