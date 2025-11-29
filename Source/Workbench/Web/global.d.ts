// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// <reference types="chai" />

declare module '*.css';
declare module '*.svg' {
    const content: string;
    export default content;
}

declare module '*.png' {
    const content: string;
    export default content;
}

// Extend Chai's Assertion interface for TypeScript support
declare global {
    namespace Chai {
        interface Assertion {
            called: Assertion;
        }
    }
}
