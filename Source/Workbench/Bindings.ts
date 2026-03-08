// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { container } from 'tsyringe';
import { Bindings as ArcBindings } from '@cratis/arc.react.mvvm';
import { Constructor } from '@cratis/fundamentals';
import { INamespaces, Namespaces } from 'State/Namespaces';
import { IEventStores, EventStores } from './State/EventStores';

export class Bindings {
    static initialize() {
        ArcBindings.initialize();
        container.registerSingleton(INamespaces as Constructor<INamespaces>, Namespaces);
        container.registerSingleton(IEventStores as Constructor<IEventStores>, EventStores);
    }
}
