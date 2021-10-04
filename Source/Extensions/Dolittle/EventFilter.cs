// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Extensions.Dolittle
{
    /*
    {
        "$eventTypeId": {
            "$in": [
                "690a6c6f-e65a-4a8d-924d-2c37899bbed3",
                "1fb231fa-fd76-4180-9193-291dbaee3fff",
                "4edaff29-d305-4646-bc3d-dd18a001ee5c"
            ]
        }
        "$eventSourceId": "39915d7b-093e-49c4-b981-22e85bd571bf",
        "$occurred": {
            "$or": {
                "$gt": "2021-09-25T14:27:25.296Z"
                "$lt": "2021-09-28T14:27:25.296Z"
            }
        }
    }
    */

    public class EventFilter
    {
        public Expression? Expression { get; set; }
    }
}
