// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResult } from '../../QueryResult';

describe("when asking has data and it is empty array", () => {
    const queryResult = new QueryResult<any>({
        validationResults: [],
        data: []
    }, Object, true);

    it('should consider to not having data', () => queryResult.hasData.should.be.false);
});
