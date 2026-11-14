// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceImplementationGenerator.given;

[Orleans.Alias(nameof(ICasingGrain))]
public interface ICasingGrain : Orleans.IGrainWithStringKey
{
    Task<bool> HasData();
}
