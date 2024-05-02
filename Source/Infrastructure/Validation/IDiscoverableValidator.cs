// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Validation;

/// <summary>
/// Defines a discoverable validator that can be discovered and automatically hooked up.
/// </summary>
/// <typeparam name="T">Type of object the validator is for.</typeparam>
/// <remarks>
/// The type needs to in addition implement fluent validation AbstractValidator or something that
/// implements it.
/// </remarks>
public interface IDiscoverableValidator<T>;
