import { appendSnapshot, getHistory, pruneStaleElections } from './turnoutHistory';

describe('turnoutHistory', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('returns an empty history for an unknown election', () => {
    expect(getHistory('unknown')).toEqual([]);
  });

  it('appends a snapshot and persists it', () => {
    appendSnapshot('election-1', 10);
    const history = getHistory('election-1');

    expect(history).toHaveLength(1);
    expect(history[0].v).toBe(10);
    expect(typeof history[0].t).toBe('number');
  });

  it('deduplicates consecutive snapshots with an unchanged vote count', () => {
    appendSnapshot('election-1', 10);
    appendSnapshot('election-1', 10);
    appendSnapshot('election-1', 10);

    expect(getHistory('election-1')).toHaveLength(1);
  });

  it('appends a new point once the vote count changes', () => {
    appendSnapshot('election-1', 10);
    appendSnapshot('election-1', 12);

    const history = getHistory('election-1');
    expect(history).toHaveLength(2);
    expect(history[1].v).toBe(12);
  });

  it('caps stored history at 300 points, dropping the oldest', () => {
    for (let i = 0; i < 305; i += 1) {
      appendSnapshot('election-1', i);
    }

    const history = getHistory('election-1');
    expect(history).toHaveLength(300);
    expect(history[0].v).toBe(5);
    expect(history[299].v).toBe(304);
  });

  it('keeps separate histories per election', () => {
    appendSnapshot('election-1', 5);
    appendSnapshot('election-2', 9);

    expect(getHistory('election-1')).toHaveLength(1);
    expect(getHistory('election-2')).toHaveLength(1);
    expect(getHistory('election-1')[0].v).toBe(5);
    expect(getHistory('election-2')[0].v).toBe(9);
  });

  it('prunes history for elections not in the active id list', () => {
    appendSnapshot('election-1', 5);
    appendSnapshot('election-2', 9);
    localStorage.setItem('some_other_key', 'untouched');

    pruneStaleElections(['election-1']);

    expect(getHistory('election-1')).toHaveLength(1);
    expect(getHistory('election-2')).toHaveLength(0);
    expect(localStorage.getItem('some_other_key')).toBe('untouched');
  });
});
