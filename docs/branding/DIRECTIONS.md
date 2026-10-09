# Brand directions

Status: direction C (Lootlark) was adopted on 2026-10-09; see [SUMMARY.md](SUMMARY.md). Only `assets/c-lootlark/` is committed, so links to the `a-quarterhall` and `b-shelfplay` asset folders below point at local-only files. Original phase 1 text follows. Phase 1 proposal by Kestrel. These are three genuinely different directions, each tied to names from [NAMING.md](NAMING.md). Nothing in the front end has been changed. Each direction ends with a CSS token block that the existing Bootstrap 5.3 front end could adopt with one extra stylesheet.

| | A. Arcade after hours | B. The curated shelf | C. Bright and chirpy |
|---|---|---|---|
| Name | **Quarterhall** (alt. Joystock) | **Shelfplay** (alt. Nightshelf) | **Lootlark** (alt. Emberkey) |
| Mood | Neon arcade at night, dark first | Independent bookshop, warm paper | Sunny, friendly, a cartoon bird |
| Default theme | Dark | Light | Light |
| Display / text font | Bungee / Rubik | Fraunces / Inter | Baloo 2 / Nunito Sans |
| Corner radius | 6 px (tight, cabinet-like) | 10 px (soft book corners) | 16 px (pill-like, chunky) |
| Assets | [assets/a-quarterhall/](assets/a-quarterhall/) | [assets/b-shelfplay/](assets/b-shelfplay/) | [assets/c-lootlark/](assets/c-lootlark/) |

Each asset folder contains:

- `mark.svg`: the square symbol.
- `logo.svg`: full-colour lockup.
- `logo-mono.svg`: one colour, with the inner shapes knocked out by a mask so the logo prints in a single ink.
- `logo-reversed.svg`: on the dark surface.
- `palette.svg`: swatch sheet.
- `hero.svg`: storefront home mock.
- `card.svg`: product card in three states (with cover, no cover, loading).
- PNG renders: `logo-600.png`, `logo-reversed-600.png`, `logo-64.png` and `logo-32.png` (lockup at 64 and 32 px high), `mark-64.png`, `mark-32.png`, `hero.png`, `card.png` and `palette.png`.

The SVGs pull their fonts from Google Fonts with an `@import`. Opened directly in a browser they render as designed. In tools that block external fonts they fall back to the system stacks named in the file. The game titles in the mocks ("Starfall Rally", "Tidebound" and so on) are made up, and the cover art is abstract placeholder shapes, not real game art.

How I checked contrast: the WCAG 2.x relative-luminance formula, computed in Python. The bar is 4.5:1 (AA) for body text and 3:1 (AA large) for headings of 24 px and up (or 18.66 px bold). Every pair listed as used passes. The pairs that fail are listed with their fix.

---

## A. Arcade after hours: Quarterhall

**Idea.** A neon arcade hall after closing time, with you holding the last quarter. The mark is a coin that is also a Q, with a coin slot through its middle. The site is dark first, because arcades and launchers are dark. A light theme exists for daytime and printing.

**Personality:** playful, nostalgic, punchy.

**Tone of voice.** Short, arcade-flavoured lines, capitals for calls to action, and one game joke per screen at most. The joke never replaces information.

| Moment | String |
|---|---|
| Empty cart | "No coins in the slot yet. Find a game and press start." |
| Checkout success | "PLAYER 1 READY. Payment received. Your game code appears on the order page in a moment." |
| 404 | "GAME OVER for this page. It does not exist. Continue? Back to the hall." |
| Add to cart | Button label "INSERT COIN", with `aria-label="Add {game} to cart"`. Toast: "Added to cart." |
| Error | "Something jammed the machine. We could not load the games. Try again." |

**Palette**

| Role | Hex | Notes |
|---|---|---|
| Primary (light mode) | `#C2185B` | Arcade magenta. Buttons and links on light. |
| Primary (dark mode) | `#FF5C9A` | Neon pink. Buttons and links on dark, with ink labels. |
| Accent | `#00C2D1` | Cyan. Chips, coin slot, kicker text on dark. Never text on light. |
| Accent as text on light | `#007C86` | Darkened cyan, for the rare case accent text sits on light. |
| Neutral: ink | `#16121F` | Body text (light), labels on bright fills. |
| Neutral: muted | `#5E5873` (light) / `#A39DB8` (dark) | Secondary text. |
| Success | `#157F45` (light) / `#4ADE80` (dark) | |
| Warning | `#F5A524` fill, `#8A5A00` as text on light | |
| Danger | `#C62828` (light) / `#FF7070` (dark) | |
| Surface light | `#FBF8FF` | |
| Surface dark | `#110E1A` | Default page background. Cards use `#1C1828`. |
| Text on dark | `#EDE9F7` | |

**Contrast (computed)**

| Pair | Colours | Ratio | Needs | Result |
|---|---|---|---|---|
| Body text, light | `#16121F` on `#FBF8FF` | 17.51:1 | AA | pass |
| Muted text, light | `#5E5873` on `#FBF8FF` | 6.40:1 | AA | pass |
| Button label on primary | `#FFFFFF` on `#C2185B` | 5.87:1 | AA | pass |
| Link text, light | `#C2185B` on `#FBF8FF` | 5.59:1 | AA | pass |
| Ink on accent chip | `#16121F` on `#00C2D1` | 8.45:1 | AA | pass |
| Success text, light | `#157F45` on `#FBF8FF` | 4.81:1 | AA | pass |
| Warning text, light | `#8A5A00` on `#FBF8FF` | 5.64:1 | AA | pass |
| Danger text, light | `#C62828` on `#FBF8FF` | 5.35:1 | AA | pass |
| Label on danger button | `#FFFFFF` on `#C62828` | 5.62:1 | AA | pass |
| Body text, dark | `#EDE9F7` on `#110E1A` | 15.98:1 | AA | pass |
| Muted text, dark | `#A39DB8` on `#110E1A` | 7.33:1 | AA | pass |
| Link text, dark | `#FF5C9A` on `#110E1A` | 6.58:1 | AA | pass |
| Button label, dark | `#16121F` on `#FF5C9A` | 6.36:1 | AA | pass |
| Accent kicker on dark | `#00C2D1` on `#110E1A` | 8.75:1 | AA large | pass |
| Success / warning / danger text, dark | `#4ADE80` / `#F5A524` / `#FF7070` on `#110E1A` | 10.94 / 9.34 / 7.08:1 | AA | pass |
| Primary hover button | `#FFFFFF` on `#A4144D` | 7.48:1 | AA | pass |
| **Failing:** cyan accent as text on light | `#00C2D1` on `#FBF8FF` | 2.07:1 | AA | **fail**. Fix: use `#007C86` (4.72:1) |
| **Failing:** white on cyan | `#FFFFFF` on `#00C2D1` | 2.18:1 | AA | **fail**. Fix: ink labels on cyan (8.45:1) |
| **Failing:** raw warning as text | `#F5A524` on `#FBF8FF` | 1.94:1 | AA | **fail**. Fix: `#8A5A00` for text; amber only as a fill |
| **Failing:** light primary on dark | `#C2185B` on `#110E1A` | 3.25:1 | AA | **fail** for body text. Fix: dark mode switches to `#FF5C9A` (6.58:1) |

**Typography**
- Display: **Bungee** 400 (its only weight). A signage face drawn for vertical and arcade-style headlines. Use it only for the logo, headings 24 px and up, and button labels in caps. It is too loud for running text.
- Text: **Rubik** 400 / 500 / 700. Its slightly rounded corners echo pixel-era softness, it stays very legible at 14 to 16 px, and it has tabular figures for prices.
- Scale: 14 / 16 / 20 / 28 / 40 / 56 px. Body line height 1.55.

**Shape language**
- Radius 6 px on cards, inputs and buttons (10 px for large surfaces). Cabinet-like and tight.
- Shadows: almost none on dark. Elevation is a **glow**: `0 0 0 1px rgba(255,92,154,.35), 0 8px 24px rgba(255,92,154,.18)`. This keeps the current site's hover glow idea, in brand colours.
- Icons: 2 px stroke, square caps, 24 px grid (Lucide or Tabler, outline). No filled icons except the cart badge.

**Motion**
- Snappy and stepped: 120 ms ease-out for hovers, 180 ms for entering. One playful exception: the "INSERT COIN" button drops a 6 px coin into the cart badge (300 ms, `steps(4)` for a pixel feel).
- Respect `prefers-reduced-motion`. Turn off the coin drop and keep colour changes only.
- No flashing and no scanline animation (photosensitivity); scanlines appear only as static texture.

**Cover art imagery**
- Cards: 3:4 cover, full bleed inside a 10 px inset, dark card body. Title in Rubik 700, genre as a cyan chip, price right-aligned.
- Hover: lift 4 px, magenta glow, and the cover scales to 1.03 inside its clip. Keyboard focus gets the same glow plus a 2 px outline.
- Skeleton: `#2A2538` blocks with a slow 1.6 s opacity pulse (reusing the existing `placeholder-glow` class).
- No cover: a dashed magenta frame with the game's initials in Bungee and "Cover coming soon". Never show a broken image icon.

**Bootstrap tokens** (also keeps the current dark navbar, since `data-bs-theme="dark"` already sits on it):

```css
/* Quarterhall tokens for Bootstrap 5.3. Load after bootstrap.min.css. */
:root,
[data-bs-theme="light"] {
  --brand-font-display: 'Bungee', 'Arial Black', sans-serif;
  --bs-font-sans-serif: 'Rubik', system-ui, -apple-system, 'Segoe UI', Arial, sans-serif;
  --bs-body-font-family: var(--bs-font-sans-serif);

  --bs-primary: #C2185B;
  --bs-primary-rgb: 194, 24, 91;
  --brand-accent: #00C2D1;
  --brand-accent-rgb: 0, 194, 209;
  --brand-accent-text: #007C86;   /* accent when used as text on light surfaces */
  --bs-success: #157F45;
  --bs-success-rgb: 21, 127, 69;
  --bs-warning: #F5A524;
  --bs-warning-rgb: 245, 165, 36;
  --bs-danger: #C62828;
  --bs-danger-rgb: 198, 40, 40;
  --bs-warning-text-emphasis: #8A5A00;

  --bs-body-bg: #FBF8FF;
  --bs-body-bg-rgb: 251, 248, 255;
  --bs-body-color: #16121F;
  --bs-body-color-rgb: 22, 18, 31;
  --bs-emphasis-color: #16121F;
  --bs-secondary-color: #5E5873;
  --bs-tertiary-bg: #F2EDFA;
  --bs-border-color: #E2DCEE;
  --bs-link-color: #C2185B;
  --bs-link-color-rgb: 194, 24, 91;
  --bs-link-hover-color: #A4144D;
  --bs-link-hover-color-rgb: 164, 20, 77;

  --bs-border-radius: 6px;
  --bs-border-radius-lg: 10px;
  --bs-focus-ring-color: rgba(194, 24, 91, .35);
}

[data-bs-theme="dark"] {
  --bs-primary: #FF5C9A;
  --bs-primary-rgb: 255, 92, 154;
  --brand-accent-text: #00C2D1;
  --bs-success: #4ADE80;
  --bs-success-rgb: 74, 222, 128;
  --bs-danger: #FF7070;
  --bs-danger-rgb: 255, 112, 112;
  --bs-warning-text-emphasis: #F5A524;

  --bs-body-bg: #110E1A;
  --bs-body-bg-rgb: 17, 14, 26;
  --bs-body-color: #EDE9F7;
  --bs-body-color-rgb: 237, 233, 247;
  --bs-emphasis-color: #EDE9F7;
  --bs-secondary-color: #A39DB8;
  --bs-tertiary-bg: #1C1828;
  --bs-border-color: #2C2640;
  --bs-link-color: #FF5C9A;
  --bs-link-color-rgb: 255, 92, 154;
  --bs-link-hover-color: #EDE9F7;
  --bs-link-hover-color-rgb: 237, 233, 247;
}

/* Bootstrap compiles button colours from Sass, so --bs-primary alone does not
   recolour .btn-primary. These rules map the buttons onto the tokens. */
.btn-primary {
  --bs-btn-bg: var(--bs-primary);
  --bs-btn-border-color: var(--bs-primary);
  --bs-btn-color: #FFFFFF;
  --bs-btn-hover-bg: #A4144D;
  --bs-btn-hover-border-color: #A4144D;
  --bs-btn-hover-color: #FFFFFF;
  --bs-btn-active-bg: #911244;
  --bs-btn-active-border-color: #911244;
  --bs-btn-disabled-bg: var(--bs-primary);
  --bs-btn-disabled-border-color: var(--bs-primary);
}
[data-bs-theme="dark"] .btn-primary {
  --bs-btn-color: #16121F;
  --bs-btn-hover-bg: #EDE9F7;
  --bs-btn-hover-border-color: #EDE9F7;
  --bs-btn-hover-color: #16121F;
  --bs-btn-active-bg: #EDE9F7;
  --bs-btn-active-color: #16121F;
}
.btn-outline-primary {
  --bs-btn-color: var(--bs-primary);
  --bs-btn-border-color: var(--bs-primary);
  --bs-btn-hover-bg: var(--bs-primary);
  --bs-btn-hover-border-color: var(--bs-primary);
  --bs-btn-hover-color: var(--bs-body-bg);
  --bs-btn-active-bg: var(--bs-primary);
  --bs-btn-active-color: var(--bs-body-bg);
}
h1, h2, h3, .navbar-brand, .display-1, .display-2, .display-3 {
  font-family: var(--brand-font-display);
}
```

---

## B. The curated shelf: Shelfplay

**Idea.** A small independent bookshop, but for games. Warm paper background, a serif with personality, staff-pick energy. The mark is two book spines and a play triangle standing on a shelf. It says "curated", not "infinite catalogue". That is honest for a demo shop with a small catalogue.

**Personality:** calm, curated, warm.

**Tone of voice.** A knowledgeable shop assistant: full sentences, sentence case, gentle humour, no shouting and no exclamation marks except on success.

| Moment | String |
|---|---|
| Empty cart | "Your cart is empty. The shelf is full, though: have a browse." |
| Checkout success | "Thank you, your order is in! We are fetching your game code; it will be on the order page shortly." |
| 404 | "This page is not on our shelf. Maybe it was moved. Back to the shop." |
| Add to cart | Button "Add to cart". Toast: "Added Tidebound to your cart. View cart." |
| Error | "We could not load the shelf just now. Please try again in a moment." |

**Palette**

| Role | Hex | Notes |
|---|---|---|
| Primary (light mode) | `#0F5C57` | Deep bottle green-teal: bookbinding cloth. |
| Primary (dark mode) | `#5CC3B8` | Light teal, with ink labels. |
| Accent | `#E3A33B` | Amber: gilt lettering, staff-pick stickers, genre chips. |
| Accent as text on light | `#8C5A0E` | |
| Neutral: ink | `#1F1B16` | Warm near-black. |
| Neutral: muted | `#5F574C` (light) / `#ABA293` (dark) | |
| Success | `#2E6B3F` (light) / `#7BC88F` (dark) | |
| Warning | `#E3A33B` fill, `#7F5208` as text on light | Shares the accent hue; differentiate with an icon. |
| Danger | `#A8322A` (light) / `#F08A7E` (dark) | |
| Surface light | `#F7F3EC` | Paper. Hero band `#EFE8DC`, cards `#FFFFFF`. |
| Surface dark | `#15130F` | Reading-lamp dark mode. |
| Text on dark | `#EEE8DD` | |

**Contrast (computed)**

| Pair | Colours | Ratio | Needs | Result |
|---|---|---|---|---|
| Body text, light | `#1F1B16` on `#F7F3EC` | 15.48:1 | AA | pass |
| Muted text, light | `#5F574C` on `#F7F3EC` | 6.43:1 | AA | pass |
| Button label on primary | `#FFFFFF` on `#0F5C57` | 7.81:1 | AA | pass |
| Link text, light | `#0F5C57` on `#F7F3EC` | 7.06:1 | AA | pass |
| Ink on accent chip | `#1F1B16` on `#E3A33B` | 7.80:1 | AA | pass |
| Success text, light | `#2E6B3F` on `#F7F3EC` | 5.77:1 | AA | pass |
| Warning text, light | `#7F5208` on `#F7F3EC` | 6.10:1 | AA | pass |
| Danger text, light | `#A8322A` on `#F7F3EC` | 6.02:1 | AA | pass |
| Label on danger button | `#FFFFFF` on `#A8322A` | 6.66:1 | AA | pass |
| Body text, dark | `#EEE8DD` on `#15130F` | 15.21:1 | AA | pass |
| Muted text, dark | `#ABA293` on `#15130F` | 7.35:1 | AA | pass |
| Link text, dark | `#5CC3B8` on `#15130F` | 8.79:1 | AA | pass |
| Button label, dark | `#15130F` on `#5CC3B8` | 8.79:1 | AA | pass |
| Accent on dark | `#E3A33B` on `#15130F` | 8.45:1 | AA large (passes AA too) | pass |
| Success / danger text, dark | `#7BC88F` / `#F08A7E` on `#15130F` | 9.28 / 7.63:1 | AA | pass |
| Primary hover button | `#FFFFFF` on `#0C4E49` | 9.53:1 | AA | pass |
| **Failing:** amber accent as text on light | `#E3A33B` on `#F7F3EC` | 1.98:1 | AA | **fail**. Fix: `#8C5A0E` (5.30:1) |
| **Failing:** white on amber | `#FFFFFF` on `#E3A33B` | 2.19:1 | AA | **fail**. Fix: ink labels on amber (7.80:1) |
| **Failing:** light primary on dark | `#0F5C57` on `#15130F` | 2.38:1 | AA | **fail**. Fix: dark mode switches to `#5CC3B8` (8.79:1) |

**Typography**
- Display: **Fraunces** 600 and 600 italic (the logo's "play"), 700 for the hero. A soft, slightly quirky old-style serif with an optical-size axis, so it stays crisp at 24 px and characterful at 64 px. The serif is what separates this direction from every launcher-style store.
- Text: **Inter** 400 / 500 / 600. Neutral, superb at small sizes, tabular numbers for prices and order totals.
- Scale: 14 / 16 / 18 / 24 / 36 / 54 px. Body line height 1.6. Headings in sentence case.

**Shape language**
- Radius 10 px (14 px for large panels). Soft like a book corner.
- Shadows: paper-like and low, `0 1px 2px rgba(31,27,22,.06), 0 4px 12px rgba(31,27,22,.06)`. A card lifts to `0 10px 24px rgba(31,27,22,.12)`.
- Icons: 1.5 px stroke, round caps (Lucide). Occasional amber "staff pick" sticker as the only decorative shape.

**Motion**
- Calm: 200 ms `cubic-bezier(.2,.7,.2,1)` for hovers, 260 ms for page sections fading up 8 px. A featured cover tilts 2 degrees, like a book pulled from a shelf.
- Respect `prefers-reduced-motion`: no tilt and no translate, opacity only.

**Cover art imagery**
- Cards: white card on paper, 3:4 cover with a 10 px inset, as if a book cover were framed. Title in Inter 600 (Fraunces only on detail pages), amber genre chip, price right-aligned.
- Hover: lift 2 px, shadow deepens, title underlines in teal. No glow; that belongs to direction A.
- Skeleton: `#E9E6EF` blocks with a slow left-to-right sheen (1.8 s).
- No cover: paper tile with a dashed teal frame, the initials in Fraunces and "Cover coming soon".

**Bootstrap tokens** (light by default; remove `data-bs-theme="dark"` from the navbar so it uses the paper surface):

```css
/* Shelfplay tokens for Bootstrap 5.3. Load after bootstrap.min.css. */
:root,
[data-bs-theme="light"] {
  --brand-font-display: 'Fraunces', Georgia, serif;
  --bs-font-sans-serif: 'Inter', system-ui, -apple-system, 'Segoe UI', Arial, sans-serif;
  --bs-body-font-family: var(--bs-font-sans-serif);

  --bs-primary: #0F5C57;
  --bs-primary-rgb: 15, 92, 87;
  --brand-accent: #E3A33B;
  --brand-accent-rgb: 227, 163, 59;
  --brand-accent-text: #8C5A0E;   /* accent when used as text on light surfaces */
  --bs-success: #2E6B3F;
  --bs-success-rgb: 46, 107, 63;
  --bs-warning: #E3A33B;
  --bs-warning-rgb: 227, 163, 59;
  --bs-danger: #A8322A;
  --bs-danger-rgb: 168, 50, 42;
  --bs-warning-text-emphasis: #7F5208;

  --bs-body-bg: #F7F3EC;
  --bs-body-bg-rgb: 247, 243, 236;
  --bs-body-color: #1F1B16;
  --bs-body-color-rgb: 31, 27, 22;
  --bs-emphasis-color: #1F1B16;
  --bs-secondary-color: #5F574C;
  --bs-tertiary-bg: #EFE8DC;
  --bs-border-color: #E1D9CC;
  --bs-link-color: #0F5C57;
  --bs-link-color-rgb: 15, 92, 87;
  --bs-link-hover-color: #0C4E49;
  --bs-link-hover-color-rgb: 12, 78, 73;

  --bs-border-radius: 10px;
  --bs-border-radius-lg: 14px;
  --bs-focus-ring-color: rgba(15, 92, 87, .35);
}

[data-bs-theme="dark"] {
  --bs-primary: #5CC3B8;
  --bs-primary-rgb: 92, 195, 184;
  --brand-accent-text: #E3A33B;
  --bs-success: #7BC88F;
  --bs-success-rgb: 123, 200, 143;
  --bs-danger: #F08A7E;
  --bs-danger-rgb: 240, 138, 126;
  --bs-warning-text-emphasis: #E3A33B;

  --bs-body-bg: #15130F;
  --bs-body-bg-rgb: 21, 19, 15;
  --bs-body-color: #EEE8DD;
  --bs-body-color-rgb: 238, 232, 221;
  --bs-emphasis-color: #EEE8DD;
  --bs-secondary-color: #ABA293;
  --bs-tertiary-bg: #211E18;
  --bs-border-color: #2E2A23;
  --bs-link-color: #5CC3B8;
  --bs-link-color-rgb: 92, 195, 184;
  --bs-link-hover-color: #EEE8DD;
  --bs-link-hover-color-rgb: 238, 232, 221;
}

/* Bootstrap compiles button colours from Sass, so --bs-primary alone does not
   recolour .btn-primary. These rules map the buttons onto the tokens. */
.btn-primary {
  --bs-btn-bg: var(--bs-primary);
  --bs-btn-border-color: var(--bs-primary);
  --bs-btn-color: #FFFFFF;
  --bs-btn-hover-bg: #0C4E49;
  --bs-btn-hover-border-color: #0C4E49;
  --bs-btn-hover-color: #FFFFFF;
  --bs-btn-active-bg: #0B4541;
  --bs-btn-active-border-color: #0B4541;
  --bs-btn-disabled-bg: var(--bs-primary);
  --bs-btn-disabled-border-color: var(--bs-primary);
}
[data-bs-theme="dark"] .btn-primary {
  --bs-btn-color: #15130F;
  --bs-btn-hover-bg: #EEE8DD;
  --bs-btn-hover-border-color: #EEE8DD;
  --bs-btn-hover-color: #15130F;
  --bs-btn-active-bg: #EEE8DD;
  --bs-btn-active-color: #15130F;
}
.btn-outline-primary {
  --bs-btn-color: var(--bs-primary);
  --bs-btn-border-color: var(--bs-primary);
  --bs-btn-hover-bg: var(--bs-primary);
  --bs-btn-hover-border-color: var(--bs-primary);
  --bs-btn-hover-color: var(--bs-body-bg);
  --bs-btn-active-bg: var(--bs-primary);
  --bs-btn-active-color: var(--bs-body-bg);
}
h1, h2, h3, .navbar-brand, .display-1, .display-2, .display-3 {
  font-family: var(--brand-font-display);
}
```

---

## C. Bright and chirpy: Lootlark

**Idea.** The friendliest shop in the street. A little lark built from a price tag (the tag's hole is its eye, the point is its head, with a yellow beak) carries your loot. Bright cobalt and sunshine yellow, chunky rounded shapes. It aims at casual players and makes the checkout feel light and safe.

**Personality:** cheerful, quick, generous.

**Tone of voice.** Upbeat, second person, short verbs ("Grab it", "Find it"), light bird puns used sparingly (at most one per flow). Never cutesy on errors or payments.

| Moment | String |
|---|---|
| Empty cart | "Your nest is empty. Let's find something fun to put in it." |
| Checkout success | "You're all set! Payment received. Your game code is on its way to your order page." |
| 404 | "This page flew the nest. Let's get you back home." |
| Add to cart | Button "Grab it", with `aria-label="Add {game} to cart"`. Toast: "In your cart!" |
| Error | "Something went wrong on our side. We couldn't load the games. Please try again." |

**Palette**

| Role | Hex | Notes |
|---|---|---|
| Primary (light mode) | `#2B4FD8` | Cobalt blue. |
| Primary (dark mode) | `#7C9BFF` | Periwinkle, with ink labels. |
| Accent | `#FFC83D` | Sunshine yellow: the beak, chips, highlights. |
| Accent as text on light | `#8A6200` | |
| Neutral: ink | `#1A2140` | Navy ink. |
| Neutral: muted | `#56607F` (light) / `#9AA4C4` (dark) | |
| Success | `#137A43` (light) / `#4CD38A` (dark) | |
| Warning | `#F59E0B` fill, `#8A4B00` as text on light | |
| Danger | `#C8323A` (light) / `#FF7A80` (dark) | |
| Surface light | `#F5F7FC` | Cards `#FFFFFF`. |
| Surface dark | `#0E1324` | |
| Text on dark | `#E8ECF8` | |

**Contrast (computed)**

| Pair | Colours | Ratio | Needs | Result |
|---|---|---|---|---|
| Body text, light | `#1A2140` on `#F5F7FC` | 14.67:1 | AA | pass |
| Muted text, light | `#56607F` on `#F5F7FC` | 5.80:1 | AA | pass |
| Button label on primary | `#FFFFFF` on `#2B4FD8` | 6.54:1 | AA | pass |
| Link text, light | `#2B4FD8` on `#F5F7FC` | 6.10:1 | AA | pass |
| Ink on accent chip | `#1A2140` on `#FFC83D` | 10.17:1 | AA | pass |
| Success text, light | `#137A43` on `#F5F7FC` | 5.03:1 | AA | pass |
| Warning text, light | `#8A4B00` on `#F5F7FC` | 6.35:1 | AA | pass |
| Danger text, light | `#C8323A` on `#F5F7FC` | 4.93:1 | AA | pass |
| Label on danger button | `#FFFFFF` on `#C8323A` | 5.29:1 | AA | pass |
| Body text, dark | `#E8ECF8` on `#0E1324` | 15.64:1 | AA | pass |
| Muted text, dark | `#9AA4C4` on `#0E1324` | 7.46:1 | AA | pass |
| Link text, dark | `#7C9BFF` on `#0E1324` | 7.03:1 | AA | pass |
| Button label, dark | `#0E1324` on `#7C9BFF` | 7.03:1 | AA | pass |
| Accent on dark | `#FFC83D` on `#0E1324` | 11.94:1 | AA large (passes AA too) | pass |
| Success / warning / danger text, dark | `#4CD38A` / `#F59E0B` / `#FF7A80` on `#0E1324` | 9.67 / 8.60 / 7.34:1 | AA | pass |
| Primary hover button | `#FFFFFF` on `#2443B7` | 8.20:1 | AA | pass |
| **Failing:** yellow accent as text on light | `#FFC83D` on `#F5F7FC` | 1.44:1 | AA | **fail**. Fix: `#8A6200` (5.12:1) |
| **Failing:** white on yellow | `#FFFFFF` on `#FFC83D` | 1.55:1 | AA | **fail**. Fix: navy ink labels on yellow (10.17:1) |
| **Failing:** raw warning as text | `#F59E0B` on `#F5F7FC` | 2.00:1 | AA | **fail**. Fix: `#8A4B00` |
| **Failing:** light primary on dark | `#2B4FD8` on `#0E1324` | 2.82:1 | AA | **fail**. Fix: dark mode switches to `#7C9BFF` (7.03:1) |

**Typography**
- Display: **Baloo 2** 700 / 800. Round, heavy and friendly, with good Latin Extended coverage (Spanish accents, "ñ" and "¿¡" are all there). Use it for the logo, headings and big prices.
- Text: **Nunito Sans** 400 / 600 / 700. The sans sibling of rounded Nunito: friendly but not childish, and it reads well in forms and the order summary.
- Scale: 14 / 16 / 20 / 28 / 44 / 64 px. Body line height 1.55.

**Shape language**
- Radius 16 px (22 px large, full pills for chips). Chunky and soft.
- Shadows: soft and coloured, `0 6px 16px rgba(43,79,216,.10)`. On hover, `0 14px 28px rgba(43,79,216,.16)`.
- Icons: 2 px stroke, round caps and joins (Phosphor Regular or Lucide). Filled variants are allowed for the active nav item.

**Motion**
- Springy but short: 180 ms with a small overshoot (`cubic-bezier(.34,1.56,.64,1)`) on buttons and the cart badge. When something is added to the cart, the lark mark in the navbar hops once (2 px, 240 ms).
- Respect `prefers-reduced-motion`: no hop and no overshoot, 120 ms fades only.

**Cover art imagery**
- Cards: white, 16 px radius, 3:4 cover with a 10 px inset and rounded corners. Yellow genre chip, Baloo price, full-width "Grab it" button.
- Hover: lift 4 px with a blue-tinted shadow, the cover scales to 1.04, and the button darkens.
- Skeleton: `#E9E6EF` rounded blocks with a sheen (1.4 s).
- No cover: pale tile with a dashed blue frame, the initials in Baloo, and "Cover coming soon". A later iteration could use the lark holding an empty frame.

**Bootstrap tokens** (light by default; remove `data-bs-theme="dark"` from the navbar, or keep it for a navy navbar, since the dark tokens are on-brand):

```css
/* Lootlark tokens for Bootstrap 5.3. Load after bootstrap.min.css. */
:root,
[data-bs-theme="light"] {
  --brand-font-display: 'Baloo 2', 'Trebuchet MS', sans-serif;
  --bs-font-sans-serif: 'Nunito Sans', system-ui, -apple-system, 'Segoe UI', Arial, sans-serif;
  --bs-body-font-family: var(--bs-font-sans-serif);

  --bs-primary: #2B4FD8;
  --bs-primary-rgb: 43, 79, 216;
  --brand-accent: #FFC83D;
  --brand-accent-rgb: 255, 200, 61;
  --brand-accent-text: #8A6200;   /* accent when used as text on light surfaces */
  --bs-success: #137A43;
  --bs-success-rgb: 19, 122, 67;
  --bs-warning: #F59E0B;
  --bs-warning-rgb: 245, 158, 11;
  --bs-danger: #C8323A;
  --bs-danger-rgb: 200, 50, 58;
  --bs-warning-text-emphasis: #8A4B00;

  --bs-body-bg: #F5F7FC;
  --bs-body-bg-rgb: 245, 247, 252;
  --bs-body-color: #1A2140;
  --bs-body-color-rgb: 26, 33, 64;
  --bs-emphasis-color: #1A2140;
  --bs-secondary-color: #56607F;
  --bs-tertiary-bg: #EAEEF9;
  --bs-border-color: #DDE3F2;
  --bs-link-color: #2B4FD8;
  --bs-link-color-rgb: 43, 79, 216;
  --bs-link-hover-color: #2443B7;
  --bs-link-hover-color-rgb: 36, 67, 183;

  --bs-border-radius: 16px;
  --bs-border-radius-lg: 22px;
  --bs-focus-ring-color: rgba(43, 79, 216, .35);
}

[data-bs-theme="dark"] {
  --bs-primary: #7C9BFF;
  --bs-primary-rgb: 124, 155, 255;
  --brand-accent-text: #FFC83D;
  --bs-success: #4CD38A;
  --bs-success-rgb: 76, 211, 138;
  --bs-danger: #FF7A80;
  --bs-danger-rgb: 255, 122, 128;
  --bs-warning-text-emphasis: #F59E0B;

  --bs-body-bg: #0E1324;
  --bs-body-bg-rgb: 14, 19, 36;
  --bs-body-color: #E8ECF8;
  --bs-body-color-rgb: 232, 236, 248;
  --bs-emphasis-color: #E8ECF8;
  --bs-secondary-color: #9AA4C4;
  --bs-tertiary-bg: #182039;
  --bs-border-color: #232C47;
  --bs-link-color: #7C9BFF;
  --bs-link-color-rgb: 124, 155, 255;
  --bs-link-hover-color: #E8ECF8;
  --bs-link-hover-color-rgb: 232, 236, 248;
}

/* Bootstrap compiles button colours from Sass, so --bs-primary alone does not
   recolour .btn-primary. These rules map the buttons onto the tokens. */
.btn-primary {
  --bs-btn-bg: var(--bs-primary);
  --bs-btn-border-color: var(--bs-primary);
  --bs-btn-color: #FFFFFF;
  --bs-btn-hover-bg: #2443B7;
  --bs-btn-hover-border-color: #2443B7;
  --bs-btn-hover-color: #FFFFFF;
  --bs-btn-active-bg: #203BA2;
  --bs-btn-active-border-color: #203BA2;
  --bs-btn-disabled-bg: var(--bs-primary);
  --bs-btn-disabled-border-color: var(--bs-primary);
}
[data-bs-theme="dark"] .btn-primary {
  --bs-btn-color: #0E1324;
  --bs-btn-hover-bg: #E8ECF8;
  --bs-btn-hover-border-color: #E8ECF8;
  --bs-btn-hover-color: #0E1324;
  --bs-btn-active-bg: #E8ECF8;
  --bs-btn-active-color: #0E1324;
}
.btn-outline-primary {
  --bs-btn-color: var(--bs-primary);
  --bs-btn-border-color: var(--bs-primary);
  --bs-btn-hover-bg: var(--bs-primary);
  --bs-btn-hover-border-color: var(--bs-primary);
  --bs-btn-hover-color: var(--bs-body-bg);
  --bs-btn-active-bg: var(--bs-primary);
  --bs-btn-active-color: var(--bs-body-bg);
}
h1, h2, h3, .navbar-brand, .display-1, .display-2, .display-3 {
  font-family: var(--brand-font-display);
}
```

---

## Adopting a direction later (not done in this phase)

1. Copy the chosen token block into `frontend/GameStore.Frontend/src/brand.css` and import it after Bootstrap in `main.tsx`.
2. Add the two Google Fonts with a `<link>` in `index.html` (`display=swap`, only the listed weights).
3. Replace the text brand "Game Store" in `NavMenu.tsx` with the SVG lockup, add `mark-32.png` and an SVG favicon, and replace the hard-coded `#5f4dee` hover in `Home.module.css` with the token.
4. Replace the "Loading..." text with skeleton cards, and add the no-cover fallback to the card `<img>` (`onError`).
5. Rename user-facing copy only (title, README heading, page strings). `GameStore.*` namespaces stay.
