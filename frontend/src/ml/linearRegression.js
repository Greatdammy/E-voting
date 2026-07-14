export function fitLine(points) {
  const n = points.length;
  if (n < 2) {
    return null;
  }

  const x0 = points[0].x;
  const xs = points.map((point) => point.x - x0);
  const ys = points.map((point) => point.y);

  const meanX = xs.reduce((sum, x) => sum + x, 0) / n;
  const meanY = ys.reduce((sum, y) => sum + y, 0) / n;

  let numerator = 0;
  let denominator = 0;
  for (let i = 0; i < n; i += 1) {
    numerator += (xs[i] - meanX) * (ys[i] - meanY);
    denominator += (xs[i] - meanX) ** 2;
  }

  const slope = denominator === 0 ? 0 : numerator / denominator;
  const intercept = meanY - slope * meanX;

  return { slope, intercept, x0 };
}

export function predict(line, x) {
  if (!line) {
    return null;
  }
  return line.intercept + line.slope * (x - line.x0);
}
