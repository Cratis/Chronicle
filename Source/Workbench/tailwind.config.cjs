// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** @type {import('tailwindcss').Config} */
module.exports = {
    content: {
        files: [
            "./index.html",
            "./**/*.{js,ts,jsx,tsx}",
            "./node_modules/primereact/**/*.{js,ts,jsx,tsx}",
        ]
    },
    theme: {
        extend: {},
    },
    plugins: [],
}
