import Icon from "../components/Icon";
import type { SessionUser } from "../auth/auth";
import { useState } from "react";
import {
  Breakdown,
  Comparison,
  Count,
  NoticeList,
  Trend,
} from "../features/transparency/components";
import {
  categories,
  money,
  months,
  notices,
  total,
} from "../features/transparency/data";
import IssueLayout from "./IssueLayout";
import "../styles/Dashboard.css";
import "../styles/TransparencyPage.css";

function MonthlyOverview({ index }: { index: number }) {
  const current = months[index];
  const previous = months[index - 1];
  const sum = total(current.amounts);
  const previousTotal = total(previous.amounts);
  const average =
    total(months.slice(0, index + 1).map((month) => total(month.amounts))) /
    (index + 1);
  const changes = categories.map((category, i) => ({
    ...category,
    change: current.amounts[i] - previous.amounts[i],
  }));
  const largestChange = [...changes].sort(
    (a, b) => Math.abs(b.change) - Math.abs(a.change),
  )[0];
  return (
    <div className="tp-calm tp-reveal">
      <section
        className="tp-calm-overview"
        aria-label="Monthly expense summary"
      >
        <div className="tp-monthly-total">
          <p className="tp-eyebrow">
            Association expenses · {current.name} 2026
          </p>
          <div className="tp-big-number">
            <Count value={sum} /> <span>MDL</span>
          </div>
          <Comparison index={index} />
        </div>
        <div className="tp-month-comparison">
          {[previous, current].map((month) => (
            <div className="tp-month-row" key={month.name}>
              <div>
                <span>{month.name}</span>
                <strong>{money(total(month.amounts))}</strong>
              </div>
              <div className="tp-bar-track" aria-hidden="true">
                <div
                  className="tp-bar"
                  style={{
                    width: `${(total(month.amounts) / Math.max(sum, previousTotal)) * 100}%`,
                    background: month === current ? "#087d6b" : "#86aaa0",
                  }}
                />
              </div>
            </div>
          ))}
        </div>
      </section>
      <div className="tp-calm-grid">
        <section className="tp-panel" aria-labelledby="breakdown-heading">
          <div className="tp-section-heading">
            <div>
              <p className="tp-eyebrow">The monthly picture</p>
              <h2 id="breakdown-heading">Where the money went</h2>
            </div>
            <span className="tp-unit">MDL</span>
          </div>
          <Breakdown index={index} />
          <div className="tp-total-row">
            <strong>Total expenses</strong>
            <strong>{money(sum)}</strong>
          </div>
        </section>
        <section className="tp-calm-notices" aria-labelledby="notices-heading">
          <div className="tp-section-heading">
            <div>
              <p className="tp-eyebrow">
                September 2026
              </p>
              <h2 id="notices-heading">Good to know</h2>
            </div>
            <span
              className="tp-count-badge"
              aria-label={`${notices.length} notices`}
            >
              {notices.length}
            </span>
          </div>
          <NoticeList />
        </section>
      </div>
      <section
        className="tp-history tp-panel"
        aria-labelledby="history-heading"
      >
        <div className="tp-history-chart">
          <div className="tp-section-heading">
            <div>
              <p className="tp-eyebrow">March – {current.name} 2026</p>
              <h2 id="history-heading">A little more perspective</h2>
            </div>
            <span className="tp-unit">MDL</span>
          </div>
          <Trend index={index} />
          <div className="tp-average">
            <span>
              Average monthly spending <small>March–{current.name}</small>
            </span>
            <strong>{money(average)}</strong>
          </div>
        </div>
        <div className="tp-changes">
          <p className="tp-eyebrow">Compared with {previous.name}</p>
          <h3>What changed this month?</h3>
          <p className="tp-change-intro">
            {largestChange.name} had the biggest change:{" "}
            <strong>
              {money(Math.abs(largestChange.change))}{" "}
              {largestChange.change < 0 ? "less" : "more"}
            </strong>{" "}
            than {previous.name}.
          </p>
          <ul>
            {changes.map((item) => (
              <li key={item.id}>
                <span>
                  <i style={{ background: item.color }} />
                  {item.name}
                </span>
                <strong>
                  {item.change === 0
                    ? "No change"
                    : `${item.change < 0 ? "−" : "+"}${money(Math.abs(item.change))}`}
                </strong>
              </li>
            ))}
          </ul>
          <div className="tp-change-total">
            <span>Overall change</span>
            <strong>
              {sum < previousTotal ? "−" : "+"}
              {money(Math.abs(sum - previousTotal))}
            </strong>
          </div>
        </div>
      </section>
    </div>
  );
}

export default function TransparencyPage({ user }: { user: SessionUser }) {
  const queryMonth = new URLSearchParams(window.location.search).get("month");
  const [index, setIndex] = useState(queryMonth === "july" ? 4 : 5);
  function changeMonth(value: number) {
    setIndex(value);
    const url = new URL(window.location.href);
    url.searchParams.set("month", value === 4 ? "july" : "august");
    window.history.replaceState(null, "", url);
  }
  function selectMonth(value: number) {
  changeMonth(value);

  const menu = document.getElementById("expense-month-menu");
  menu?.setAttribute("hidden", "");
}
  return (
    <IssueLayout user={user} activePage="transparency">
      <a className="tp-skip" href="#transparency-main">
        Skip to content
      </a>
      <main className="tp-main tp-page" id="transparency-main">
          <div className="tp-page-heading">
            <div>
              <h1>Transparency, at a glance.</h1>
            </div>
            <div className="tp-month-control">
  <label htmlFor="expense-month-button">Expense month</label>

  <div className="tp-month-picker">
    <Icon name="calendar" size={18} />

    <button
      id="expense-month-button"
      className="tp-month-trigger"
      type="button"
      onClick={() => {
        const menu = document.getElementById("expense-month-menu");
        menu?.toggleAttribute("hidden");
      }}
      aria-haspopup="listbox"
      aria-expanded="false"
    >
      <span>{index === 5 ? "August 2026" : "July 2026"}</span>
      <span className="tp-month-arrow" aria-hidden="true">⮟</span>
    </button>

          <div id="expense-month-menu" className="tp-month-menu" role="listbox" hidden>
              <button
                type="button"
                role="option"
                aria-selected={index === 5}
                onClick={() => selectMonth(5)}
              >
                August 2026
              </button>

              <button
                type="button"
                role="option"
                aria-selected={index === 4}
                onClick={() => selectMonth(4)}
              >
                July 2026
              </button>
              </div>
            </div>
          </div>
          </div>
          <MonthlyOverview key={index} index={index} />
      </main>
    </IssueLayout>
  );
}
