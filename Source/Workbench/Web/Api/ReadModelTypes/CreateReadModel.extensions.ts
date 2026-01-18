// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CreateReadModel } from './CreateReadModel';

// Extend the CreateReadModel interface to include schema property
// This is needed for runtime schema assignment
declare module './CreateReadModel' {
    interface ICreateReadModel {
        schema?: string;
    }

    interface CreateReadModel {
        schema: string;
    }
}

// Initialize the schema property on the prototype
Object.defineProperty(CreateReadModel.prototype, 'schema', {
    get(this: CreateReadModel & { _schema?: string }) {
        return this._schema || '';
    },
    set(this: CreateReadModel & { _schema?: string }, value: string) {
        this._schema = value;
        this.propertyChanged('schema');
    },
    enumerable: true,
    configurable: true
});
