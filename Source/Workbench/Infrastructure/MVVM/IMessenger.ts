// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Defines a system for publishing and subscribing to messages.
 */
export abstract class IMessenger {

    abstract publish<T>(message: T): void;
    abstract subscribe<T>(callback: (message: T) => void): void;
    abstract unsubscribe<T>(callback: (message: T) => void): void;
}
