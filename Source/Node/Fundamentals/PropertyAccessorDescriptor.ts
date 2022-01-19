// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PropertyAccessor } from './PropertyAccessor';

/**
 * Represents the descriptor of a specific property accessor
 */
export class PropertyAccessorDescriptor {

    /**
     * Creates an instance of {PropertyAccessorDescriptor}.
     * @param {PropertyAccessor} _accessor - The actual accessor for accessing the property.
     * @param {Array<string>} _segments - The segments representing the path of the property accessor within an instance.
     */
    constructor(private readonly _accessor: PropertyAccessor, private readonly _segments: string[]) {
    }

    /**
     * Gets the actual accessor.
     * @returns {PropertyAccessor}
     */
    get accessor(): PropertyAccessor {
        return this._accessor;
    }

    /**
     * Gets the segments that constitute the deep path within the object instance for accessing the underlying property.
     * @returns {ReadonlyArray<string>}
     */
    get segments(): readonly string[] {
        return this._segments;
    }

    /**
     * Gets the full path to the property
     * @returns {string}
     */
    get path(): string {
        return this._segments.join('.');
    }
}
