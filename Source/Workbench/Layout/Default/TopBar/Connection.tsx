// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ImConnection } from "react-icons/im";

import { tw } from 'typewind';

export const Connection = () => {
    return (<div className={tw.flex_1}>
        <div className={tw.flex.justify_end.gap_3}>
            <ImConnection size={25} />
        </div>
    </div>)
};
