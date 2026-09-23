import Icon from "../../components/Icon";
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
        <details className="tp-notice" key={notice.id}>
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
    </div>
  );
}
