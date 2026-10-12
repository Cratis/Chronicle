// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// The exception that is thrown when a custom provider cannot identify an opaque migration requiring new owning-registry binding.
/// </summary>
/// <param name="providerType">The custom migration provider type.</param>
public class ExplicitMigrationBindingNotSupported(Type providerType)
    : Exception($"'{providerType.FullName}' provides an opaque representation of an explicit migration that requires owning-registry binding; its original recipe cannot be identified safely.");
