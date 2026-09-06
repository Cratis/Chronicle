// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration;

/// <summary>
/// The exception that is thrown by a reactor that is asked to fail, so a specification can observe how a failing
/// partition behaves.
/// </summary>
public class DeliberateReactorFailure() : Exception("The reactor was asked to fail so the specification can observe the failure");
