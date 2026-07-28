# Free Rule-Based Help Widget (Frontend-Only)

Status: IMPLEMENTED (2026-07-28).

## Context
Revisits the voter-assistant idea from `ai-voter-assistant.md` (dropped
because it needed a paid Anthropic API call), rebuilt as a free, rule-based
FAQ widget instead. No LLM, no ML, no backend, no external API — a keyword
match against a small fixed set of canned answers about this app's own
flows.

## Decision: not branded as "AI"
Deliberately kept separate from the fuchsia "AI accent" + `Sparkles` styling
used by the two real AI/ML features (Integrity Guard, Turnout Forecast), so
those two keep sole ownership of the "notable AI feature" claim. This widget
uses a plain `HelpCircle` icon and the ordinary indigo brand button color,
and is labeled "Help" in the UI, not "Assistant" or "AI."

## What shipped
- **`frontend/src/help/helpTopics.js`** — a `topics` array of
  `{ keywords, response }` plus `findAnswer(message)`, a pure lowercase
  substring match returning either the matched response or a fixed fallback
  listing what it can help with. Topics: registering, logging in, voting,
  election status meanings, the confirmation receipt/hash, live results,
  dark mode.
- **`frontend/src/components/HelpWidget.jsx`** — floating bottom-right
  toggle + expandable panel, local `useState` message list (no persistence,
  resets on reload — a deliberate simplicity choice), text input +
  send-button/submit. Fully synchronous, zero network calls.
- **`App.jsx`** — mounted once, unconditionally, alongside `NavBar`, so it's
  visible on every route including Login/Register.
- **`frontend/src/components/HelpWidget.test.jsx`** — 4 tests (closed by
  default, opens on toggle, matches a known keyword, falls back for an
  unrecognized question), using `fireEvent` to match the existing
  `LoginPage.test.jsx` convention (there is no `@testing-library/user-event`
  dependency in this project — checked before writing the test, since an
  earlier draft assumed it existed).

No new npm dependency (lucide-react already provides `HelpCircle`/`X`/`Send`).
No backend files touched.

## Follow-up: matching was too narrow (2026-07-28, same day)
First version used "first topic with any single keyword hit" over ~7 topics
with only a handful of exact phrases each — in practice, almost any
naturally-phrased question missed and fell through to the fallback, which
read as "gives the same answer for everything." Reworked `findAnswer` to:
- **Score every topic** (count of matching keyword phrases) and return the
  highest-scoring one, instead of stopping at the first topic with any hit
  — so array order no longer silently decides borderline matches.
- **Normalize both the message and each keyword identically** (lowercase,
  strip punctuation) before comparing — the first punctuation-stripping
  attempt only normalized the message, which silently broke every keyword
  containing an apostrophe (e.g. "can't log in"); caught and fixed before
  it shipped.
- **Much broader keyword lists per topic**, plus two new topics (admin/
  officer roles; a greeting/"what can you do" catch-all) to reduce how
  often ordinary openers hit the fallback.
This remains a ceiling, not a fix to "real" understanding — it's still
substring/keyword scoring, not NLU, and stays that way deliberately (no
paid API). Told the user this plainly rather than implying otherwise.

## Verification
- `npm test`: 26/26 passing (2 new tests added specifically for the
  expanded-matching behavior — a non-exact phrasing and a greeting — on top
  of the original 24).
- `npm run build`: succeeds.
- Manual check on `/login` before authenticating: not yet performed by
  either party — recommended before fully trusting the "helps before login"
  behavior in practice.
