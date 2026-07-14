import { fitLine, predict } from './linearRegression';

describe('fitLine', () => {
  it('returns null with fewer than 2 points', () => {
    expect(fitLine([])).toBeNull();
    expect(fitLine([{ x: 0, y: 1 }])).toBeNull();
  });

  it('fits an exact line through points with x starting at 0', () => {
    const points = [
      { x: 0, y: 1 },
      { x: 1, y: 3 },
      { x: 2, y: 5 },
      { x: 3, y: 7 }
    ];

    const line = fitLine(points);

    expect(line.slope).toBeCloseTo(2);
    expect(predict(line, 5)).toBeCloseTo(11);
  });

  it('fits an exact line through points with a large, non-zero x offset', () => {
    const points = [
      { x: 100, y: 5 },
      { x: 101, y: 7 },
      { x: 102, y: 9 }
    ];

    const line = fitLine(points);

    expect(line.slope).toBeCloseTo(2);
    expect(predict(line, 105)).toBeCloseTo(15);
  });

  it('falls back to a flat line (slope 0) when all x values are identical', () => {
    const points = [
      { x: 10, y: 4 },
      { x: 10, y: 8 },
      { x: 10, y: 6 }
    ];

    const line = fitLine(points);

    expect(line.slope).toBe(0);
    expect(predict(line, 999)).toBeCloseTo(6);
  });
});

describe('predict', () => {
  it('returns null when given no line', () => {
    expect(predict(null, 5)).toBeNull();
  });
});
