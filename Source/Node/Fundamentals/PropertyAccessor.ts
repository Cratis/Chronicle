// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * The delegate for representing accessing a property
 */
export type PropertyAccessor<T = any> = (instance: T) => any;
