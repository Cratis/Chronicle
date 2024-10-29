// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
import { Namespace } from '../../Api/Namespaces/Namespace';

/**
 * Event that gets triggered when the current namespace changes.
 */

export class CurrentNamespaceChanged {
    constructor(readonly namespace: Namespace) { }
}
