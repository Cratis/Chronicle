// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

module.exports = function (wallaby) {
    return {
        files: [
            { pattern: './package.json', instrument: false },
            '!**/*.d.ts*',
            '!**/for_*/**/when_*.ts*',
            '!**/for_*/**/and_*.ts*',
            '**/*.ts*'
        ],

        tests: [
            '!**/node_modules/**/*',
            '!**/*.d.ts*',
            '**/for_*/**/when_*.ts*',
            '**/for_*/**/and_*.ts*'
        ],

        testFramework: 'mocha',

        compilers: {
            '**/*.ts?(x)': wallaby.compilers.typeScript({
                module: 'commonjs',
                jsx: 'React'
            })
        },

        setup: wallaby => {
            const { glob } = require('glob');
            const fs = require('fs');
            const path = require('path');
            const chai = require('chai');
            const sinon = require('sinon');

            const mocha = wallaby.testFramework;

            chai.use(require('sinon-chai'));

            // setup sinon hooks
            mocha.suite.beforeEach('sinon before', function () {
                if (null == this.sinon) {
                    this.sinon = sinon.createSandbox();
                }
            });
            mocha.suite.afterEach('sinon after', function () {
                if (this.sinon && 'function' === typeof this.sinon.restore) {
                    this.sinon.restore();
                }
            });

            global.expect = require('chai').expect;
            var should = chai.should();
        },

        env: {
            type: 'node',
            runner: 'node'
        },

        workers: { recycle: true }
    }
}
