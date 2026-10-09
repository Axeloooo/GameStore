# Branding: two-minute summary

> **Decision (2026-10-09).** The owner chose **Lootlark**, direction **C "Bright and chirpy"**, after a four-voter committee (C 9 points, B 8, A 7). Directions A (Quarterhall) and B (Shelfplay) were not adopted; their assets stay local and are not committed. Only `assets/c-lootlark/` is in the repository. The recommendation below is the phase 1 proposal, kept as a record of the reasoning before the vote. Nothing here is a trademark, domain or availability claim.

Phase 1 proposal by Kestrel (saved by the orchestrator). Nothing in the code has been renamed or restyled. The details are in NAMING.md (36-name longlist, shortlist of 8, availability) and DIRECTIONS.md (three directions with palettes, contrast, type, motion and Bootstrap tokens). Logos and mocks are in assets/. Figma: https://www.figma.com/design/N6DuwYGTxL5G8teDmhwQ7x (three frames; text uses a fallback font, so the local PNGs are the true-to-type reference).

## Recommendation (Kestrel, before the committee vote)

**Name: Shelfplay. Direction: B, "The curated shelf".**

Why:
- **It fits what the product is.** A small, hand-picked catalogue of digital games with an honest checkout. "Curated shelf" makes a small catalogue a feature instead of a gap.
- **It stands out.** Almost every game store is a dark launcher with neon. A warm paper surface, a teal and amber palette and a serif display face (Fraunces) look different from all of them in a portfolio.
- **It is the cleanest name checked.** Of the eight names, Shelfplay is the only one where both GitHub handles returned 404 and shelfplay.com returned no DNS records. Lookups are not proof of availability; see the risks below.
- **It needs no explaining** in an English README, and it has no awkward meaning in Spanish.
- **Contrast has room to spare.** The tightest text pair is 5.77:1, against 4.5 required.

**Runner-up: Lootlark with direction C.** Warmer and more memorable, with a mascot-ready mark (a price tag that is also a bird) and free GitHub handles. But lootlark.com is registered, and the playful voice is harder to keep tasteful on payment and error screens.

Direction A (Quarterhall, a neon arcade) is the strongest mood of the three. It is also the closest to what game stores already look like, and "quarter" is a US-centric word.

## Top 3 risks
1. **Trademark not cleared.** No trademark search has been done for any name. Before going public, search USPTO, EUIPO, WIPO and OEPM in classes 9, 35 and 41. A free handle or an empty DNS answer says nothing about trademarks.
2. **Domain availability is a guess.** An empty DNS answer only suggests shelfplay.com is unregistered. shelfplay.store is already taken. Confirm with a registrar's read-only search before committing to the name.
3. **"Shelf" has small costs.** It hints at physical boxes, and Spanish speakers tend to say "chelf". Direction B is also light-first, so adopting it means changing the current dark navbar, which is slightly more front-end work than direction A.

## Open questions for the owner
1. **Taste.** Which mood is "you": calm and curated (B), playful (C) or neon arcade (A)? Would you rather have a Spanish name (Jugada is the wildcard)?
2. **Public or private.** Will the repository and a demo be public? If the name stays inside a private portfolio, trademark and domain matter much less.
3. **Trademark.** Are you willing to run the free register searches before launch? Recommended for any public use.
4. **Domain budget.** Do you want a domain at all? If yes, is roughly the price of one .com or .dev a year acceptable? Premium or for-sale domains (such as emberkey.com on Afternic) are out of scope.
5. **Repository rename.** Should the GitHub repository and the README title change, or only the UI? Code namespaces stay GameStore.* either way.

## What exists now
- NAMING.md, DIRECTIONS.md and this file.
- assets/c-lootlark/. The folder has the mark, the logo in colour, one-colour and reversed versions, a palette sheet, a hero mock and a product card mock as SVG, plus PNG renders at 600, 64 and 32 px.
