// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResult } from '../../QueryResult';

describe("when asking has data and it is an array with items", () => {
    const queryResult = new QueryResult<any>([{}, {}], true);

    it('should consider having data', () => queryResult.hasData.should.be.true);
});
