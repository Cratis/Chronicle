// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Own-property check that is safe for keys such as `__proto__` and `constructor`.
 * @param value The object to inspect.
 * @param key The property name.
 * @returns True when the object has the property itself.
 */
export const hasOwn = (value: object, key: string): boolean => Object.prototype.hasOwnProperty.call(value, key);
