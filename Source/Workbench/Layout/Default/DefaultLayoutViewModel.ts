// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { injectable } from 'tsyringe';

@injectable()
export class DefaultLayoutViewModel {
    constructor() {
        this.namespaces = [
            'My-First-Namespace',
            'My-Second-Namespace',
            'My-Third-Namespace'
        ];

        this.currentNamespace = this.namespaces[0];
    }

    currentNamespace: string = '';
    namespaces: string[] = [];
}
