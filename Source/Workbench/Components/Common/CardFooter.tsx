// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { FaRotate, FaXmark, FaWhmcs } from 'react-icons/fa6';

import { tw } from 'typewind';

export const CardFooter = () => {
    return (
        <div className={tw.flex.justify_end.gap_2}>
            <div className={tw.flex.gap_1}>
                5 <FaXmark size={25} />
            </div>
            <div className={tw.flex.gap_1}>
                15 <FaRotate size={20} />
            </div>
            <div className={tw.flex.gap_1}>
                <FaWhmcs size={25} />
            </div>
        </div>
    );
};
