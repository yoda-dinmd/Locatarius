import { useEffect, useState } from "react";
import {
  categories,
  money,
  months,
  notices,
  number,
  percentage,
  total,
} from "./data";

export function Icon({ name, size = 20 }: { name: string; size?: number }) {
  const paths: Record<string, string> = {
    home: "M3 10 12 3l9 7v10H3Z M9 20v-7h6v7",
    board: "M4 4h16v16H4Z M8 8h8M8 12h8M8 16h4",
    repair: "m14 6 4 4 M4 20l9-9 M14 3a6 6 0 0 0 7 7l-4-4 1-4Z M4 16l4 4",
    clean: "m14 3-4 10 M6 12l8 3 1 6H3Z M7 16l-1 5M11 17v4 M19 3v6M16 6h6",
    elevator: "M5 3h14v18H5Z M9 9l3-3 3 3M9 15l3 3 3-3",
    light: "M9 17h6M9 21h6 M8 14a6 6 0 1 1 8 0l-1 3H9Z",
    calendar: "M4 5h16v16H4ZM8 2v6M16 2v6M4 10h16",
    check: "m5 12 4 4L19 6",
    issue: "M12 3 2 21h20ZM12 9v5M12 17v1",
  };
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.6"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <path d={paths[name] || paths.board} />
    </svg>
  );
}

export function Count({ value }: { value: number }) {
  const [display, setDisplay] = useState(value);
  useEffect(() => {
    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;
    let frame: number;
    const start = performance.now();
    const tick = (time: number) => {
      const progress = Math.min((time - start) / 480, 1);
      setDisplay(Math.round(value * (1 - (1 - progress) ** 3)));
      if (progress < 1) frame = requestAnimationFrame(tick);
    };
    frame = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(frame);
  }, [value]);
  return (
    <>
      <span aria-hidden="true">{number(display)}</span>
      <span className="tp-sr">{number(value)}</span>
    </>
  );
}

export function Comparison({ index }: { index: number }) {
  const current = total(months[index].amounts);
  const previous = total(months[index - 1].amounts);
  const lower = current < previous;
  return (
    <span className="tp-comparison">
      {lower ? "↘" : "↗"}{" "}
      {percentage((Math.abs(current - previous) / previous) * 100)}%{" "}
      {lower ? "lower" : "higher"} than {months[index - 1].name}{" "}
      <span>({money(Math.abs(current - previous))})</span>
    </span>
  );
}

export function Breakdown({ index }: { index: number }) {
  const values = months[index].amounts;
  const sum = total(values);
  return (
    <div className="tp-breakdown">
      {categories.map((category, i) => (
        <div className="tp-expense" key={category.id}>
          <span className="tp-category-icon" style={{ color: category.color }}>
            <Icon name={category.icon} />
          </span>
          <div className="tp-expense-body">
            <div className="tp-expense-heading">
              <strong>{category.name}</strong>
              <strong>{money(values[i])}</strong>
            </div>
            <div className="tp-bar-track">
              <div
                className="tp-bar"
                style={{
                  width: `${(values[i] / sum) * 100}%`,
                  background: category.color,
                }}
              />
            </div>
            <div className="tp-expense-caption">
              <span>{category.detail}</span>
              <span>{percentage((values[i] / sum) * 100)}%</span>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}

export function NoticeList() {
  return (
    <div className="tp-notices">
      {notices.map((notice) => (
        <details
          className={`tp-notice tp-notice-${notice.priority.toLowerCase()}`}
          key={notice.id}
        >
          <summary>
            <span className="tp-date">
              <strong>{notice.day}</strong>
              <small>{notice.month}</small>
            </span>
            <span className="tp-notice-copy">
              <span
                className={`tp-tag tp-tag-${notice.priority.toLowerCase()}`}
              >
                {notice.priority}
              </span>
              <strong>{notice.title}</strong>
              <span>{notice.summary}</span>
            </span>
            <span className="tp-expand" aria-hidden="true">
              +
            </span>
          </summary>
          <div className="tp-notice-detail">
            <p>{notice.detail}</p>
            <small>{notice.location} · Association administration</small>
          </div>
        </details>
      ))}
    </div>
  );
}

export function Trend({ index }: { index: number }) {
  const series = months.slice(0, index + 1);
  const [active, setActive] = useState<number | null>(null);
  const values = series.map((month) => total(month.amounts));
  const ceiling = Math.ceil(Math.max(...values) / 10000) * 10000;
  const focused =
    active !== null && active < series.length ? active : series.length - 1;
  return (
    <div className="tp-trend">
      <div className="tp-trend-readout" aria-live="polite">
        <span>{series[focused].name} 2026 · Total expenses</span>
        <strong>{money(values[focused])}</strong>
      </div>
      <div className="tp-trend-plot">
        <div className="tp-axis">
          <span>{number(ceiling)}</span>
          <span>{number(ceiling / 2)}</span>
          <span>0 MDL</span>
        </div>
        <div className="tp-columns">
          {series.map((month, i) => (
            <button
              type="button"
              className={`tp-column ${focused === i ? "is-active" : ""}`}
              key={month.short}
              onMouseEnter={() => setActive(i)}
              onMouseLeave={() => setActive(null)}
              onFocus={() => setActive(i)}
              onBlur={() => setActive(null)}
              onClick={() => setActive(i)}
              aria-label={`${month.name} 2026: ${money(values[i])}`}
            >
              <span className="tp-chart-tooltip" aria-hidden="true">
                {month.name}
                <strong>{money(values[i])}</strong>
              </span>
              <span className="tp-column-space">
                <span
                  className="tp-column-bar"
                  style={{
                    height: `${(values[i] / ceiling) * 100}%`,
                  }}
                />
              </span>
              <span>{month.short}</span>
            </button>
          ))}
        </div>
      </div>
      <p className="tp-footnote">
        Hover, tap or focus a month to inspect its expenses.
      </p>
    </div>
  );
}
