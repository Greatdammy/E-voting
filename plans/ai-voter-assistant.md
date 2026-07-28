# AI Voter Assistant (Chat Widget)

Status: DROPPED (2026-07-28). This plan required a paid Anthropic API key
billed per token; the user does not want any feature that incurs real
charges. No source files were created. Not being replaced with a free
alternative — the project's AI story stands on the Election Integrity Guard
(and, if built, the frontend modernization pass) alone. Kept here only as a
record of what was considered and why it was rejected.

## Goal
A floating chat widget, visible on voter-facing pages, that helps voters
navigate the app: how registration/login works, how to cast a vote, what the
`Upcoming`/`Active`/`Closed` status badges mean, how to read the confirmation
hash/receipt after voting, and basic accessibility guidance. This is the
"looks AI-powered" half of the two features — the most visually obvious
AI-infused surface in the redesigned frontend.

## Hard boundary: no candidate opinions, no persuasion
Given the whole system's premise is election integrity, the assistant **must
never generate commentary comparing candidates, characterising a candidate's
platform, or expressing any preference** — that's a real risk of perceived
bias in a voting system. Scope is strictly: process/navigation help, and
verbatim retrieval of already-stored candidate `Name`/`Party` fields if asked
"who's running" (list, don't summarize or editorialize). This constraint is
enforced primarily via the system prompt, and secondarily by only ever
passing the assistant the specific DB fields it's allowed to quote — never
open-ended browsing of candidate data.

## Architecture

**New external dependency:** an LLM call, which is new to this project (no
existing AI integration in the backend calls an external API — the turnout
forecast is pure client-side math). Recommend **Claude Haiku 4.5** via the
official Anthropic .NET SDK (`Anthropic` NuGet package) — it's the
cost/latency-appropriate model for a narrow-scope FAQ/navigation assistant;
happy to use Opus if you'd rather prioritize response quality over cost.

**Server-side only — the frontend never talks to the LLM directly.** This
keeps the API key out of the browser, lets the existing RBAC and rate-limit
middleware apply uniformly, and matches the "no secrets in source" rule the
same way the JWT signing key and SendGrid key are already handled.

- **Domain**: no new entities. (Conversation history is not persisted — see
  scope below — so there's nothing to model here.)
- **Application**:
  - `IAssistantService` — `Task<string> AskAsync(string userMessage,
    IReadOnlyList<ChatTurn> recentTurns)`. `ChatTurn` DTO is `{ Role, Content
    }`, kept in Application so the interface has no Infrastructure/SDK
    dependency.
  - `AssistantRequestDto` (incoming: message + short client-held history),
    `AssistantResponseDto` (reply text).
  - FluentValidation: message length cap (e.g. 2,000 chars), non-empty.
- **Infrastructure**:
  - `AnthropicAssistantService : IAssistantService` — wraps the Anthropic
    SDK client. System prompt hard-codes the scope boundary above. Feeds in
    only the current election's public data (title, dates, status,
    candidate name/party — the same fields already returned by the public
    `GET /elections/{id}/results` and `GET /elections` endpoints) so the
    model is grounded in data the voter could already see, never anything
    privileged.
  - API key: `Anthropic:ApiKey` — environment variable / user-secrets only,
    `appsettings.json` gets a placeholder, exactly like the existing JWT/
    SendGrid key pattern.
- **API**:
  - `AssistantController` — `POST api/assistant/chat`, `[Authorize(Roles =
    "Voter")]` (matches the rest of the voter-facing surface; unauthenticated
    visitors on Login/Register get a narrower, hard-coded FAQ with no LLM
    call — see Frontend below).
  - Extend the existing rate-limit middleware to cover this endpoint at the
    same 10 requests/min/IP tier as auth endpoints, to bound cost and abuse.
  - No conversation persisted server-side beyond what's needed for a single
    request/response — the client resends the last few turns each call (a
    small, capped sliding window), so there's no new PII-retention surface
    to worry about. A one-line entry in `AuditLogs` per call (timestamp +
    UserId only, not message content) for abuse monitoring, consistent with
    how register/login attempts are already logged.

## Frontend
- `frontend/src/components/AssistantWidget.jsx` — floating bubble bottom
  right (only rendered for authenticated Voters; hidden entirely for
  Admin/Officer views and for anonymous visitors), expandable panel, typing
  indicator, capped visible history (last ~10 turns, matching the capped
  server-side window).
- `frontend/src/hooks/useAssistant.js` — posts to `/assistant/chat` via the
  existing `axiosInstance` (JWT already attached by the interceptor), holds
  local message state.
- Disclaimer inside the widget, always visible: "AI assistant for navigation
  help only — not official guidance. It can't see your ballot choices and
  won't discuss candidates beyond their name and party."
- Mounted once in `App.jsx`'s layout shell so it persists across voter
  route changes without remounting.

## Tests
- Backend: unit test `AnthropicAssistantService` against a mocked SDK client
  (assert the system prompt and grounding data are passed, no real API call
  in tests). Integration test: RBAC — Voter gets 200, Admin/anonymous get
  403/401 before any LLM call is attempted; rate limit enforced.
- Frontend: `AssistantWidget.test.jsx` — open/close, send-message flow with
  axios mocked, disclaimer always rendered, widget absent for non-Voter
  roles/anonymous state.

## Explicit out of scope
- No persisted chat history/transcripts beyond the capped client-resent
  window — nothing new to protect under a data-retention policy.
- No candidate comparison, ranking, or persuasive language, enforced by
  system prompt + limited grounding data (never given open access to the
  DB).
- No streaming response in v1 (kept as a plain request/response call) —
  can be added later if latency is a problem; not needed for a short FAQ
  reply.
