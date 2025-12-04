// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// <reference types="vitest/globals" />
/// <reference types="chai" />
/// <reference types="sinon-chai" />

declare module '*.css';
declare module '*.svg' {
    const content: string;
    export default content;
}

declare module '*.png' {
    const content: string;
    export default content;
}

// Ensure chai should is available globally
declare global {
    namespace Chai {
        interface Assertion extends LanguageChains, NumericComparison, TypeComparison {
            called: Assertion;
        }
    }
}
