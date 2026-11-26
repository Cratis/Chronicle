// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { compareContent } from '../compareContent';

const scenarios: [string, string, string, string[]][] = [
    ['equal objects with reordered members', '{"a":1,"b":2}', '{"b":2,"a":1}', []],
    ['equivalent number spellings', '{"n":1}', '{"n":1.0e0}', []],
    ['zero and negative zero', '[0]', '[-0]', []],
    ['changed scalar', '{"quantity":1}', '{"quantity":3}', ['changed /quantity']],
    ['false versus zero', '{"v":false}', '{"v":0}', ['changed /v']],
    ['empty string versus null', '{"v":""}', '{"v":null}', ['changed /v']],
    ['null versus absent', '{"v":null}', '{}', ['removed /v']],
    ['added member', '{}', '{"v":1}', ['added /v']],
    ['array index changes', '[1,2,3]', '[1,9]', ['changed /1', 'removed /2']],
    ['array grows', '[1]', '[1,2]', ['added /1']],
    ['object replaced by array', '{"v":{}}', '{"v":[]}', ['changed /v']],
    ['root scalars', '1', '2', ['changed ']],
    ['pointer escaping', '{"a/b":1,"c~d":1}', '{"a/b":2,"c~d":2}', ['changed /a~1b', 'changed /c~0d']],
    ['own __proto__ member', '{"__proto__":1}', '{"__proto__":2}', ['changed /__proto__']],
    ['constructor member', '{"constructor":1}', '{}', ['removed /constructor']]
];

for (const [name, left, right, expected] of scenarios) {
    describe(`when comparing ${name}`, () => {
        const result = compareContent(left, right);
        it('should report the expected differences', () => result.rows.map(row => `${row.kind} ${row.path}`).should.deep.equal(expected));
        it('should be complete', () => (result.limit === undefined).should.be.true);
        it('should state the outcome', () => result.outcome.should.equal(expected.length ? 'different' : 'identical'));
    });
}

describe('when an added value is a container', () => {
    const result = compareContent('{}', '{"v":{"a":1,"b":[1,2,3]}}');
    it('should summarize it without serializing its content', () => result.rows[0].after!.should.equal('object(2)'));
    it('should show the left as absent', () => (result.rows[0].before === undefined).should.be.true);
});

describe('when content is html-like text', () => {
    const result = compareContent('{"v":"a"}', '{"v":"<img src=x onerror=alert(1)>"}');
    it('should carry it as plain text', () => result.rows[0].after!.should.equal('"<img src=x onerror=alert(1)>"'));
});
