# Naming proposal

Status: phase 1 proposal by Kestrel (brand and design lead). Nothing has been renamed. The product keeps the working name "GameStore" until the owner picks a name. Backend namespaces (`GameStore.*`) stay as they are in every case, because the code is course-derived (see `CLAUDE.md`). A new name would apply only to the user-facing product, the README title, UI copy, the repository display name and the docs.

## 1. What we are naming

**The product today.** A web shop for digital game codes. Visitors browse and search a catalogue of games (name, genre, price, cover art), add them to a basket and pay through Stripe Checkout (test mode only). A background worker then assigns a game code to each paid order, and the code appears on the order details page ("Game codes will be available soon"). Admins manage the catalogue: create, edit and delete games and upload covers. There are genres but no platforms. Sign-in uses Keycloak locally and Entra ID in the cloud.

**The product's look today.** Stock Bootstrap 5.3: a dark navbar with the plain text brand "Game Store", a five-column grid of cover cards with a purple (`#5f4dee`) glow on hover, "Loading..." text instead of skeletons, and no logo, favicon or type choices. It works, but it has no character yet. That gives us room to choose.

**Who it is for.**
1. *Shoppers.* People who play casually or regularly and want a game code quickly. They care about clear prices, trustworthy checkout and getting the code. The tone should be friendly. It should not be "hardcore gamer" and it should not feel like a corporate checkout.
2. *Portfolio readers.* Recruiters, mentors and other developers who open the GitHub repository or a demo. The name has to look like a real product in a README title and should not need explaining.
3. *The owner.* A Spanish-speaking student. The name has to be easy to say in Spanish and English, and it must not carry an embarrassing meaning in either language.

**A trap to avoid.** "Key shop" brands (resellers of game keys) have a mixed reputation for grey-market keys. The name should suggest a calm, honest shop. It should not suggest "cheap keys, no questions asked".

## 2. Naming principles

1. **Sounds like a shop, not a studio or a launcher.** People should hear "a place where I get games". They should not hear "a game", "a publisher" or "a client app". If a name could plausibly be a game's title, it fails.
2. **Bilingual-safe.** Easy to say for a Spanish speaker (avoid hard clusters like "-ths", and avoid silent letters that change the meaning), easy to spell after hearing it once, and no unfortunate meaning in English or Spanish.
3. **Ownable at small scale.** A coined or compound word that a student can plausibly hold as a GitHub handle and one domain, and that is not a close lookalike of an existing game store (Steam, Epic, GOG, itch.io, Humble, Green Man Gaming, GameStop, Fanatical, G2A, Kinguin, Eneba, CDKeys, Instant Gaming, Loot Crate and similar).
4. **Has a picture in it.** The name should suggest a mark we can draw in one shape that still reads at 32 px (a coin, a shelf, a bird). That keeps the logo, favicon and UI copy consistent.

## 3. Longlist (36 names, 6 styles)

Verdicts use the lookups in section 5 plus general knowledge. "Excluded" means I would not take the name further.

| # | Name | Style | Idea | Verdict |
|---|------|-------|------|---------|
| 1 | Glint | Evocative word | The flash of something worth picking up | Excluded: GitHub org taken, every TLD in use, generic brand word |
| 2 | Curio | Evocative word | A curious little object on a shelf | Excluded: GitHub org taken, .com on MarkMonitor (a big brand holds it) |
| 3 | Marquee | Evocative word | Arcade cabinet header / theatre sign | Excluded: crowded (npm, GitHub org, domains) |
| 4 | Emberkey | Evocative compound | A warm glow plus a game key | **Shortlist** |
| 5 | Nightshelf | Evocative compound | The shelf you browse after dinner | **Shortlist** |
| 6 | Lanternplay | Evocative compound | A lantern lighting the way to a game | Longlist: pleasant but long and soft |
| 7 | Keyfold | Compound | Keys gathered in a fold | Longlist: GitHub user taken, .com and .store in use |
| 8 | Keyhaven | Compound | A safe harbour for your game keys | **Shortlist** |
| 9 | Lootshelf | Compound | A shelf of loot | Longlist: fine, but Shelfplay says it better |
| 10 | Codecrate | Compound | A crate of game codes | Excluded: echoes Loot Crate; GitHub user and most TLDs taken |
| 11 | Shelfplay | Compound | A curated shelf you can play from | **Shortlist** |
| 12 | Shelfquest | Compound | Questing through a shelf | Excluded: GitHub org taken |
| 13 | Joystock | Compound / playful | Joystick plus stock (inventory) | **Shortlist** |
| 14 | Questbox | Compound | A box of quests | Excluded: GitHub org taken; generic |
| 15 | Keydrop | Compound | Keys dropping into your account | Excluded: Key-Drop is a well-known case-opening gambling site |
| 16 | Lootlark | Playful | A lark (songbird, and "for a lark" = for fun) carrying loot | **Shortlist** |
| 17 | Questlark | Playful | Same bird, on a quest | Longlist: weaker sibling of Lootlark |
| 18 | Pixlet | Playful | A small pixel | Longlist: cute, but reads as an image tool |
| 19 | Playcrate | Playful | A crate of play | Excluded: Loot Crate lookalike; GitHub org taken |
| 20 | Bitbazaar | Playful | An 8-bit market | Excluded: npm taken, crypto connotation |
| 21 | Quarterhall | Retro arcade | The arcade hall where you drop quarters | **Shortlist** |
| 22 | Tokenhall | Retro arcade | Arcade token hall | Longlist: "token" now reads as crypto/auth |
| 23 | Coinslot | Retro arcade | The slot on an arcade cabinet | Longlist: npm taken; slot-machine (gambling) echo |
| 24 | Coinhall | Retro arcade | Coin-op hall | Excluded: an existing crypto exchange uses the name |
| 25 | Cartridge | Retro arcade | Game cartridge | Excluded: Cartridge is an existing gaming platform (cartridge.gg) |
| 26 | Lumenarcade | Retro arcade | A glowing arcade | Longlist: long, two brand ideas in one |
| 27 | Pixelhearth | Retro arcade | A pixel fireplace | Excluded: "Hearth" sits too close to Hearthstone; domains in use |
| 28 | Bitfolio | Curated | A folio of games | Longlist: reads as a portfolio or crypto tracker |
| 29 | Gamekeeper | Curated | Keeper of games (and a gamewarden pun) | Longlist: real English word, crowded in mod-manager tools |
| 30 | Savepoint | Curated | The safe spot in a game | Excluded: many existing game shops and cafes are called "Save Point" |
| 31 | Jugada | Bilingual | Spanish for "a move, a play" | **Shortlist** |
| 32 | Partida | Bilingual | Spanish for "a match, a game session" | Longlist: also means "departure" or a ledger entry; .dev in use |
| 33 | Ludora | Bilingual (Latin *ludus*) | Place of play | Longlist: .com, .dev and .store in use |
| 34 | Ludoteca | Bilingual | Spanish/Italian for a toy library | Excluded: common generic noun; GitHub org taken |
| 35 | Steamy, Epicade, GOGo, Itchy, Humbly | Lookalikes | (listed so nobody proposes them) | Excluded: lookalikes of Steam, Epic, GOG, itch.io, Humble |
| 36 | Gamestopper, Fanatica | Lookalikes | (same) | Excluded: lookalikes of GameStop, Fanatical |

## 4. Shortlist (8)

Pronunciation is given as plain English respelling. In the "Spanish" column, "fine" means I found no unfortunate or confusing meaning.

| Name | Style | Rationale (one line) | Say it / spelling risk | Tagline idea | English meaning | Spanish meaning |
|------|-------|----------------------|------------------------|--------------|-----------------|-----------------|
| **Lootlark** | Playful | A cheerful bird that brings you loot; "for a lark" means for fun. | LOOT-lark. Low risk; the double "o" is easy for Spanish speakers ("lut"). Hearers may write "Lutlark". | *Your next game, delivered in a chirp.* | Loot = rewards/treasure; lark = songbird, a bit of fun. No negatives. | No meaning; "lark" = *alondra*. Fine. |
| **Shelfplay** | Compound / curated | A small, hand-picked shelf of games you can start playing from. | SHELF-play. Medium: "shelf" ends in a cluster that Spanish speakers soften ("chelf"). Spelling is obvious. | *Games, picked like books.* | Shelf + play. Calm, bookish. Hints at physical boxes, but people also say "digital shelf". | No meaning. Fine. |
| **Quarterhall** | Retro arcade | The arcade hall where a quarter starts the game; a coin is also a natural mark. | KWOR-ter-hawl. Medium: "quarter" is US-centric (25-cent coin) and Spanish speakers may say "cuarter". 11 letters. | *Drop a quarter. Leave with a game.* | Quarter = coin, also a city district. No negatives. | "Quarter" reads like *cuarto* (room/quarter). Fine. |
| **Nightshelf** | Evocative / premium | The shelf you browse after dinner: cosy, curated, a little moody. | NITE-shelf. Medium: "-ght" is silent and Spanish speakers may spell "Naitshelf". | *Something good for tonight.* | Night + shelf. Mildly evokes nightlife; no adult meaning that I know of. | No meaning. Fine. |
| **Keyhaven** | Compound | A safe harbour for your game keys: says "trust" in a category that needs it. | KEE-hay-ven. Medium: Spanish speakers may say "kei-ja-ven". | *Your keys, safely home.* | Key + haven. Also a small village in Hampshire, UK (minor clash). | No meaning; *llave* + *refugio*. Fine. |
| **Emberkey** | Evocative compound | A warm ember plus a game key: a cosy shop that hands you keys. | EM-ber-kee. Low risk; reads naturally in Spanish ("ember" like *émber*). | *Keep the evening glowing.* | Ember = glowing coal. No negatives. | No meaning; *brasa*. Fine. |
| **Jugada** | Bilingual | Spanish for "a play / a move"; a nod to the owner's language. | hoo-GAH-dah. High for English speakers: the J sound ("juh-GAH-da") and spelling. | *Tu próxima jugada. / Your next move.* | No English meaning. | "Una buena jugada" = a good move. Watch out: *mala jugada* = a dirty trick, and *jugarreta* is close in mood. Mostly positive. |
| **Joystock** | Playful compound | Joystick plus stock: a stocked shelf of fun. | JOY-stock. Low; may be misheard as "joystick". | *Stocked with joy.* | Joy + stock; "stock" can read as shares (finance). | No meaning. Fine. |

## 5. Availability (read-only lookups, 2026-10-09)

Method, all free and read-only: `gh api users/<name>` and `gh api orgs/<name>` (404 = handle free), `dig +short <name>.<tld> A` and `NS` for `.com`, `.dev`, `.gg`, `.store`, `.shop`, and `npm view <name> name`. Nothing was bought, registered, reserved or signed up for, and nobody was contacted.

**How to read the DNS columns.** "in use" means the domain returned A and/or NS records, so it is registered (or listed for sale: Afternic and parking nameservers mean "held, maybe purchasable at a premium"). "none" means no A and no NS records came back. That *suggests* the domain is unregistered but **does not prove it**: a registered domain can have no DNS. Check with a registrar before relying on it.

| Name | GitHub user | GitHub org | .com | .dev | .gg | .store | .shop | npm |
|------|-------------|------------|------|------|-----|--------|-------|-----|
| Lootlark | free (404) | free (404) | in use (DreamHost) | none | none | none | none | free |
| Shelfplay | free (404) | free (404) | **none** | none | none | in use | none | free |
| Quarterhall | taken (personal account) | free (404) | **none** | none | none | none | none | free |
| Nightshelf | free (404) | free (404) | in use (GoDaddy) | none | none | none | none | free |
| Keyhaven | free (404) | free (404) | in use | none | none | in use (parking) | none | free |
| Emberkey | taken (personal account) | free (404) | in use (Afternic: for-sale listing) | none | none | none | none | free |
| Jugada | taken (personal account) | free (404) | in use (parked) | none | none | none | none | free |
| Joystock | taken (personal account) | free (404) | in use | none | in use (NS only) | in use | none | free |

Notes:
- `gh api users/<name>` also returns organisations. "taken (personal account)" means a person holds the handle, so a GitHub org with that exact name is not possible. A suffix such as `quarterhall-shop` would still work.
- Only **Shelfplay** and **Quarterhall** returned no records for the `.com`. Lootlark, Nightshelf and Keyhaven have free GitHub handles plus several clean TLDs (`.dev`, `.gg`, `.shop`).
- Raw lookup output for all 34 probed names is reproducible with the commands above. I have not committed it because DNS changes over time.

**Trademark clearance has NOT been done.** None of these names has been checked against trademark registers (USPTO, EUIPO, WIPO Global Brand Database, OEPM in Spain). I am not claiming any name is clear to use. Before the product goes public under a new name, the owner should run at least a free search on those registers in classes 9 (downloadable software and game codes), 35 (online retail) and 41 (entertainment).

## 6. How the shortlist maps to the design directions

See [DIRECTIONS.md](DIRECTIONS.md):

- Direction A, "Arcade after hours": **Quarterhall** (alternative: Joystock)
- Direction B, "The curated shelf": **Shelfplay** (alternative: Nightshelf)
- Direction C, "Bright and chirpy": **Lootlark** (alternative: Emberkey)

Keyhaven and Jugada are not tied to a direction. Keyhaven fits B if the owner wants "trust" first. Jugada fits A or C if the owner wants a Spanish name; the committee should treat it as a wildcard.
