const PREFIX = 'evoting_turnout_';
const MAX_POINTS = 300;

function keyFor(electionId) {
  return `${PREFIX}${electionId}`;
}

export function getHistory(electionId) {
  try {
    const raw = localStorage.getItem(keyFor(electionId));
    return raw ? JSON.parse(raw) : [];
  } catch {
    return [];
  }
}

export function appendSnapshot(electionId, totalVotes) {
  const history = getHistory(electionId);
  const last = history[history.length - 1];

  if (last && last.v === totalVotes) {
    return history;
  }

  const next = [...history, { t: Date.now(), v: totalVotes }];
  const trimmed = next.length > MAX_POINTS ? next.slice(next.length - MAX_POINTS) : next;

  try {
    localStorage.setItem(keyFor(electionId), JSON.stringify(trimmed));
  } catch {
    // localStorage unavailable or full — forecast just has less history to work with
  }

  return trimmed;
}

export function pruneStaleElections(activeElectionIds) {
  const activeIds = new Set(activeElectionIds);

  for (let i = localStorage.length - 1; i >= 0; i -= 1) {
    const key = localStorage.key(i);
    if (key && key.startsWith(PREFIX) && !activeIds.has(key.slice(PREFIX.length))) {
      localStorage.removeItem(key);
    }
  }
}
