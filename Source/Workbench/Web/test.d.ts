// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// <reference types="vitest/globals" />
/// <reference types="chai" />
/// <reference types="sinon" />
/// <reference types="sinon-chai" />
/// <reference types="mocha" />

import type * as Chai from 'chai';

declare global {
    const describe: Mocha.SuiteFunction;
    const it: Mocha.TestFunction;
    const beforeEach: Mocha.HookFunction;
    const afterEach: Mocha.HookFunction;
    const before: Mocha.HookFunction;
    const after: Mocha.HookFunction;

    namespace Chai {
        interface Assertion {
            called: Assertion;
        }
    }

    interface Object {
        should: Chai.Assertion;
    }

    interface Array<T> {
        should: Chai.Assertion;
    }

    interface Function {
        should: Chai.Assertion;
    }

    interface String {
        should: Chai.Assertion;
    }

    interface Number {
        should: Chai.Assertion;
    }

    interface Boolean {
        should: Chai.Assertion;
    }
}

export {};
