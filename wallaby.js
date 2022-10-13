module.exports = function (wallaby) {
    return {
        files: [
            { pattern: 'package.json', instrument: false },
            { pattern: 'Source/**/package.json', instrument: false },
            '!Source/**/*.d.ts*',
            '!Source/**/for_*/**/when_*.ts*',
            '!Source/**/for_*/**/and_*.ts*',
            'Source/**/*.ts*'
        ],

        tests: [
            '!Source/**/*.d.ts*',
            'Source/**/for_*/**/when_*.ts*',
            'Source/**/for_*/**/and_*.ts*'
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
            const { addAliases } = require('module-alias');
            const fs = require('fs');
            const path = require('path');

            const rootFolder = wallaby.projectCacheDir;
            const getAliases = () => {
                const aliases = {};
                const searchPath = path.join(rootFolder, 'Source/**/package.json');
                glob.sync(searchPath).forEach(packageJson => {
                    const packageName = require(packageJson).name;
                    aliases[packageName] = path.dirname(packageJson);
                });
                return aliases;
            }
            addAliases(getAliases());

            const mocha = wallaby.testFramework;

            const chai = require('chai');
            const sinon = require('sinon');

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
