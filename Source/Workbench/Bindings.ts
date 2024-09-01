// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { container } from 'tsyringe';
import { Bindings as ApplicationModelBindings } from '@cratis/applications.react.mvvm';
import { Constructor } from '@cratis/fundamentals';
import { INamespaces, Namespaces } from 'State/Namespaces';

export class Bindings {
    static initialize() {
        ApplicationModelBindings.initialize();
        container.registerSingleton(INamespaces as Constructor<INamespaces>, Namespaces);
    }
}
