// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Defines something that is equatable to something.
 *
 * @export
 * @interface IEquatable
 */
export interface IEquatable {
    /**
     * Check whether this is equal to other.
     *
     * @param {*} other
     * @returns {boolean}
     */
    equals(other: any): boolean
}
