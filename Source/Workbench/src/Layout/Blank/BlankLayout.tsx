import { Outlet } from "react-router-dom";

export const BlankLayout = () => {
    return (
        <>
            <div>
                <Outlet/>
            </div>
        </>
    );
}