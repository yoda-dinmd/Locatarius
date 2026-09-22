// Synthetic, association-wide expenses. MDL is a prototype assumption, not an API contract.
export const categories = [
  {
    id: "repairs",
    name: "Repairs",
    detail: "Entrance repairs and shared plumbing",
    color: "#087d6b",
    icon: "repair",
  },
  {
    id: "cleaning",
    name: "Cleaning",
    detail: "Stairwells, entrances and courtyard",
    color: "#62a89a",
    icon: "clean",
  },
  {
    id: "elevator",
    name: "Elevator",
    detail: "Service contract and safety checks",
    color: "#557c98",
    icon: "elevator",
  },
  {
    id: "utilities",
    name: "Common utilities",
    detail: "Shared lighting and water",
    color: "#c19752",
    icon: "light",
  },
] as const;
export const months = [
  { name: "March", short: "Mar", amounts: [11000, 7800, 6500, 9000] },
  { name: "April", short: "Apr", amounts: [12500, 8000, 6500, 8500] },
  { name: "May", short: "May", amounts: [10200, 8200, 6500, 8000] },
  { name: "June", short: "Jun", amounts: [16000, 8200, 7000, 8500] },
  { name: "July", short: "Jul", amounts: [21000, 8200, 7500, 7800] },
  { name: "August", short: "Aug", amounts: [16500, 8400, 7500, 9450] },
];
export const total = (amounts: number[]) =>
  amounts.reduce((sum, amount) => sum + amount, 0);
export const number = (amount: number) =>
  new Intl.NumberFormat("en-GB", { maximumFractionDigits: 0 }).format(amount);
export const money = (amount: number) => `${number(amount)} MDL`;
export const percentage = (amount: number) =>
  new Intl.NumberFormat("en-GB", { maximumFractionDigits: 1 }).format(amount);
export const notices = [
  {
    id: "lift",
    day: "24",
    month: "SEP",
    priority: "Important",
    title: "Elevator maintenance on Thursday",
    summary: "The elevator will be unavailable from 09:00 to 12:00.",
    detail:
      "The service team will inspect the doors and safety system in entrance A. Please plan your trips around this window. If you need help accessing your floor, contact the administrator before the visit.",
    location: "Entrance A · 24 September 2026",
  },
  {
    id: "report",
    day: "05",
    month: "SEP",
    priority: "Information",
    title: "August expenses are ready to review",
    summary: "See the monthly breakdown and what changed since July.",
    detail:
      "August association expenses total 41,850 MDL. Repairs were 4,500 MDL lower than in July; common utilities increased by 1,650 MDL. These are shared association expenses, not your individual apartment bill.",
    location: "Published 5 September 2026",
  },
  {
    id: "clean",
    day: "26",
    month: "SEP",
    priority: "Upcoming",
    title: "A fresh start for our shared courtyard",
    summary: "Scheduled cleaning on Saturday, 08:00–11:00.",
    detail:
      "Please keep the entrance and courtyard paths clear while the cleaning team works. Regular stairwell cleaning continues as usual. This visit is included in the September cleaning service; it is not an additional August expense.",
    location: "Shared courtyard · 26 September 2026",
  },
];
