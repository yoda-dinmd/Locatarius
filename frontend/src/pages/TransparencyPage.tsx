import Icon from "../components/Icon";
import type { UserRole } from "../auth/auth";
import AppSidebar from "../components/AppSidebar";
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
  percentage,
  total,
} from "../features/transparency/data";
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
          <p className="tp-footnote">
            For the whole association, not your individual bill.
          </p>
        </div>
        <div className="tp-month-comparison">
          <p className="tp-eyebrow">Two months, side by side</p>
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
          <p className="tp-footnote">
            Same four expense categories in both months.
          </p>
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
          <div className="tp-soft-note">
            <Icon name="repair" />
            <span>
              Repair fund spending:{" "}
              <strong>{percentage((current.amounts[0] / sum) * 100)}%</strong>{" "}
              of this month’s expenses. Already included in the total.
            </span>
          </div>
        </section>
        <section className="tp-calm-notices" aria-labelledby="notices-heading">
          <div className="tp-section-heading">
            <div>
              <p className="tp-eyebrow">
                From your administrator · September 2026
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
          <p className="tp-footnote">Select a notice to read the details.</p>
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
          <p className="tp-footnote">
            Changes show spending differences, not a change to your apartment
            bill.
          </p>
        </div>
      </section>
    </div>
  );
}

export default function TransparencyPage({ role }: { role?: UserRole }) {
  const queryMonth = new URLSearchParams(window.location.search).get("month");
  const [index, setIndex] = useState(queryMonth === "july" ? 4 : 5);
  function changeMonth(value: number) {
    setIndex(value);
    const url = new URL(window.location.href);
    url.searchParams.set("month", value === 4 ? "july" : "august");
    window.history.replaceState(null, "", url);
  }
  return (
    <div className="dashboard-page tp-page">
      <a className="tp-skip" href="#transparency-main">
        Skip to content
      </a>
      <header className="dashboard-header">
        <a className="dashboard-brand" href="/dashboard">
          <span className="brand-mark">L</span>
          <span>Locatarius</span>
        </a>
        <div className="tp-header-context">
          <span className="tp-demo-dot" />
          <span>Demo data</span>
          <span className="tp-header-divider">/</span>
          <span>Resident view</span>
        </div>
      </header>
      <div className="dashboard-layout">
        <AppSidebar activePage="transparency" role={role} />
        <main className="tp-main" id="transparency-main">
          <div className="tp-page-heading">
            <div>
              <p className="tp-eyebrow">
                Teilor Residence · Resident perspective
              </p>
              <h1>Transparency, at a glance.</h1>
              <p>Your association’s spending and the updates that matter.</p>
            </div>
            <div className="tp-month-control">
              <label htmlFor="expense-month">Expense month</label>
              <div>
                <Icon name="calendar" size={18} />
                <select
                  id="expense-month"
                  value={index}
                  onChange={(event) => changeMonth(Number(event.target.value))}
                >
                  <option value={5}>August 2026</option>
                  <option value={4}>July 2026</option>
                </select>
              </div>
            </div>
          </div>
          <MonthlyOverview key={index} index={index} />
          <footer className="tp-footer">
            <span>
              <Icon name="check" size={15} /> Illustrative data · All amounts in
              MDL
            </span>
            <span>
              Expenses: {months[index].name} 2026 · Notices: September 2026
            </span>
          </footer>
        </main>
      </div>
    </div>
  );
}
