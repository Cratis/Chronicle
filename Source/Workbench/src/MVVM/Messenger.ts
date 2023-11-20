// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IMessenger } from './IMessenger';

export class Messenger extends IMessenger {

    publish<T>(message: T): void {
        console.log('Publishing message', message);
    }

    subscribe<T>(callback: (message: T) => void): void {
        throw new Error('Method not implemented.');
    }

    unsubscribe<T>(callback: (message: T) => void): void {
        throw new Error('Method not implemented.');
    }
}
