// Dependency-free telemetry chart: plots altitude over the run and shades any
// window the operator console flagged as a telemetry data-link dropout.
(function () {
  const raw = document.getElementById("series-data");
  if (!raw) return;
  const data = JSON.parse(raw.textContent);
  const canvas = document.getElementById("chart");
  if (!canvas || !data.length) return;

  const ctx = canvas.getContext("2d");
  const W = canvas.width, H = canvas.height;
  const pad = { l: 56, r: 16, t: 16, b: 28 };
  const plotW = W - pad.l - pad.r;
  const plotH = H - pad.t - pad.b;

  const alts = data.map((d) => d.altitude_ft);
  const minA = Math.min(...alts), maxA = Math.max(...alts);
  const spanA = maxA - minA || 1;

  const x = (i) => pad.l + (i / Math.max(data.length - 1, 1)) * plotW;
  const y = (a) => pad.t + (1 - (a - minA) / spanA) * plotH;

  const css = getComputedStyle(document.documentElement);
  const cAccent = css.getPropertyValue("--accent").trim() || "#58a6ff";
  const cWarn = css.getPropertyValue("--warn").trim() || "#d29922";
  const cMuted = css.getPropertyValue("--muted").trim() || "#8b949e";

  // axes + a few altitude gridlines
  ctx.strokeStyle = "#30363d";
  ctx.fillStyle = cMuted;
  ctx.font = "11px -apple-system,Segoe UI,sans-serif";
  for (let g = 0; g <= 4; g++) {
    const a = minA + (spanA * g) / 4;
    const yy = y(a);
    ctx.beginPath(); ctx.moveTo(pad.l, yy); ctx.lineTo(W - pad.r, yy); ctx.stroke();
    ctx.fillText(Math.round(a).toLocaleString(), 6, yy + 4);
  }

  // altitude trace
  ctx.strokeStyle = cAccent;
  ctx.lineWidth = 2;
  ctx.beginPath();
  data.forEach((d, i) => (i ? ctx.lineTo(x(i), y(d.altitude_ft)) : ctx.moveTo(x(i), y(d.altitude_ft))));
  ctx.stroke();

  // data-link dropout: a translucent amber band over the dropped samples...
  const stepW = plotW / Math.max(data.length - 1, 1);
  ctx.fillStyle = cWarn;
  ctx.globalAlpha = 0.18;
  data.forEach((d, i) => {
    if (d.link_dropout) ctx.fillRect(x(i) - stepW / 2, pad.t, stepW, plotH);
  });
  ctx.globalAlpha = 1;

  // ...plus amber dots on the held (stale) altitude samples
  ctx.fillStyle = cWarn;
  data.forEach((d, i) => {
    if (d.link_dropout) {
      ctx.beginPath();
      ctx.arc(x(i), y(d.altitude_ft), 3, 0, Math.PI * 2);
      ctx.fill();
    }
  });

  ctx.fillStyle = cMuted;
  ctx.fillText("altitude (ft) — amber = data-link dropout (telemetry held stale)", pad.l, H - 8);
})();
