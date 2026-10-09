import React from 'react';
import { NavLink } from 'react-router-dom';

// Lootlark mark (public/lootlark-mark.svg) plus the wordmark as live text, so the
// self-hosted Baloo 2 font is used and no external font is needed.
const BrandLogo: React.FC = () => {
    return (
        <NavLink className="navbar-brand mb-0 d-flex align-items-center gap-2" to="/" aria-label="Lootlark home">
            <img src="/lootlark-mark.svg" width="36" height="36" alt="" />
            <span className="fs-3 fw-bolder lh-1">loot<span className="text-primary">lark</span></span>
        </NavLink>
    );
};

export default BrandLogo;
