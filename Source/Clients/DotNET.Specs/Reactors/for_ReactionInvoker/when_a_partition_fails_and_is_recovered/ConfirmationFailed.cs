// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_a_partition_fails_and_is_recovered;

/// <summary>
/// The exception that is thrown when sending the order confirmation fails, taking the partition down after the
/// card has already been charged.
/// </summary>
public class ConfirmationFailed() : Exception("Could not send the order confirmation");
