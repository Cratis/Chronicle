import { Namespace } from '../../Api/Namespaces/Namespace';

/**
 * Event that gets triggered when the current namespace changes.
 */

export class CurrentNamespaceChanged {
    constructor(readonly namespace: Namespace) { }
}
