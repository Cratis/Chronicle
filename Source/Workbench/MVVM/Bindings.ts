// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { container } from 'tsyringe';
import { Messenger } from './Messenger';
import { Constructor } from '@aksio/fundamentals';
import { IMessenger } from './IMessenger';

export class Bindings {
    static initialize() {
        container.registerSingleton(IMessenger as Constructor<IMessenger>, Messenger);
    }
}
