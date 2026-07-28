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

## Verification
- `npm test`: 24/24 passing (up from 20/20 — 4 new tests, no regressions).
- `npm run build`: succeeds.
- Manual check on `/login` before authenticating: not yet performed by
  either party — recommended before fully trusting the "helps before login"
  behavior in practice.
