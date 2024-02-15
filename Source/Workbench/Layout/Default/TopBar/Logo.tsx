// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { tw } from 'typewind';

export const Logo = () => {
    return (
        <div className={tw.flex.justify_evenly.h_16.items_center}>
            <div className={tw.flex_1.font_extrabold.text_2xl}>CRATIS</div>
        </div>
    );
};
