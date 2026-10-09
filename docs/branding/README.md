# Lootlark brand guide

## Decision

On 2026-10-09 the owner chose the name **Lootlark** and brand direction **C, "Bright and chirpy"**, after a four-voter committee (C 9 points, B 8, A 7). The other two directions (Quarterhall and Shelfplay) were not adopted and are not kept in the repository. Lootlark is the product brand only: the repository, the `GameStore.*` namespaces, API routes and cloud and identity names do not change.

**Nothing here is a trademark, domain or availability claim, and no trademark search has been done.** Before any public use, search the trademark registers (USPTO, EUIPO, WIPO, OEPM; classes 9, 35 and 41) and check domains with a registrar. A free GitHub handle or an empty DNS answer proves nothing about either.

## What the brand is

The name is a lark (a songbird; "for a lark" means for fun) that brings you loot. The mark is a little lark built from a price tag: the tag's hole is its eye, the point is its head, and a yellow beak completes it. The look is bright cobalt and sunshine yellow with chunky rounded shapes, aimed at casual players. Personality: cheerful, quick, generous.

## Palette

Values are the ones in [`frontend/src/styles/lootlark-tokens.css`](../../frontend/src/styles/lootlark-tokens.css). The swatch sheet is [`assets/palette.svg`](assets/palette.svg).

| Role | Light | Dark |
|---|---|---|
| Primary | `#2B4FD8` (cobalt) | `#7C9BFF` |
| Accent (fills only: beak, chips) | `#FFC83D` | `#FFC83D` |
| Accent as text | `#8A6200` | `#FFC83D` |
| Ink (text, labels on yellow) | `#1A2140` | `#E8ECF8` |
| Muted text | `#56607F` | `#9AA4C4` |
| Page surface | `#F5F7FC` (cards `#FFFFFF`) | `#0E1324` (cards `#182039`) |
| Success | `#137A43` | `#4CD38A` |
| Warning | `#F59E0B` fill, `#8A4B00` text | `#F59E0B` |
| Danger | `#C8323A` | `#FF7A80` |

Contrast (WCAG 2.x, computed when the palette was chosen; the bar is 4.5:1 for text, 3:1 for large text and focus outlines):

- Pass: ink on page 14.67, muted on page 5.80, white on primary 6.54, link on page 6.10, ink on yellow 10.17, light text on dark page 15.64, dark-mode primary on dark page 7.03.
- Do not use: yellow `#FFC83D` as text on a light surface (1.44, use `#8A6200`, 5.12), white text on yellow (1.55, use navy ink), raw warning `#F59E0B` as text on light (2.00, use `#8A4B00`), light-mode primary on the dark page (2.82, dark mode switches to `#7C9BFF`).
- Focus is a solid 2px outline in the primary colour (6.10 on the light page, 7.03 on the dark page), never a translucent ring.

## Typography

- Display: **Baloo 2**, 700 and 800. Logo, headings, big prices.
- Text: **Nunito Sans**, 400, 600 and 700. Body, forms, order summary.
- Scale 14 / 16 / 20 / 28 / 44 / 64 px, body line height 1.55.
- The front end loads both fonts from npm with Fontsource (`@fontsource-variable/baloo-2` and `@fontsource-variable/nunito-sans`, imported in `frontend/src/main.tsx`), so it makes no request to Google Fonts. The SVG files in `assets/` are different: they pull the two fonts with an `@import` of Google Fonts inside the file. That is left as is. Opened directly in a browser they render as designed; where external fonts are blocked they fall back to the system stacks named in the file.

## Logo files

| File | Use | Preview |
|---|---|---|
| [`assets/logo.svg`](assets/logo.svg) | Full-colour lockup on light surfaces | ![Lootlark logo](assets/logo.svg) |
| [`assets/logo-mono.svg`](assets/logo-mono.svg) | One ink, for print or single-colour places | ![Lootlark one-colour logo](assets/logo-mono.svg) |
| [`assets/logo-reversed.svg`](assets/logo-reversed.svg) | On the dark surface (`#0E1324`) | ![Lootlark reversed logo](assets/logo-reversed.svg) |
| [`assets/mark.svg`](assets/mark.svg) | The square lark symbol alone (icons, favicon) | ![Lootlark mark](assets/mark.svg) |

No clear-space or minimum-size rule was defined. The front end ships its own copies of the mark in `frontend/public/` (`lootlark-mark.svg`, `favicon.svg`, `favicon.png`); they are not linked to these files.

## Voice and tone

Upbeat, second person, short verbs ("Grab it", "Find it"). Light bird puns, at most one per flow. Never cutesy on errors or payments.

| Moment | Copy |
|---|---|
| Empty cart | "Your nest is empty. Let's find something fun to put in it." |
| Checkout success | "You're all set! Payment received. Your game code is on its way to your order page." |
| 404 | "This page flew the nest. Let's get you back home." |
| Add to cart | Button "Grab it" (`aria-label="Add {game} to cart"`), toast "In your cart!" |
| Error | "Something went wrong on our side. We couldn't load the games. Please try again." |

## Shape and motion

Corner radius 16 px (22 px large, full pills for chips). Soft blue-tinted shadows (`0 6px 16px rgba(43,79,216,.10)`, lifted on hover to `0 14px 28px rgba(43,79,216,.16)`). Icons with a 2px stroke and round caps. Transitions are short and are switched off under `prefers-reduced-motion`.

## Where the tokens live

[`frontend/src/styles/lootlark-tokens.css`](../../frontend/src/styles/lootlark-tokens.css) is imported in `frontend/src/main.tsx` after the fonts. Bootstrap 5.3 itself comes from the CDN link in `frontend/index.html`, so this stylesheet loads after it and wins. It overrides Bootstrap's CSS variables (`--bs-primary`, `--bs-body-bg`, `--bs-body-color`, `--bs-border-radius`, link and focus colours) for `:root` and `[data-bs-theme="light"]`, and again under `[data-bs-theme="dark"]`. Brand-only values are `--brand-*` variables (accent, ink, surface, focus, shadows, cover gradient). Bootstrap compiles button colours from Sass, so the file also maps `.btn-primary`, `.btn-outline-primary` and the pagination onto the tokens.
