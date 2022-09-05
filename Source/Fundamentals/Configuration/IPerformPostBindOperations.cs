// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Defines an interface for configuration objects to implement to be called when the configuration object is initialized.
/// </summary>
public interface IPerformPostBindOperations
{
    /// <summary>
    /// The method thats gets called after the object is initialized.
    /// </summary>
    void Perform();
}
