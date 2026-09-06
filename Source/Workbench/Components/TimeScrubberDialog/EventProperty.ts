// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * One property of an event, ready to render.
 */
export interface EventProperty {
    /** The property's name. */
    name: string;
    /** The property's value, rendered as text. */
    value: string;
}
