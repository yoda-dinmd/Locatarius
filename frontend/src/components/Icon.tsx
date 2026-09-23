const paths = {
  home: "M3 10 12 3l9 7v10H3Z M9 20v-7h6v7",
  board: "M4 4h16v16H4Z M8 8h8M8 12h8M8 16h4",
  repair: "m14 6 4 4 M4 20l9-9 M14 3a6 6 0 0 0 7 7l-4-4 1-4Z M4 16l4 4",
  clean: "m14 3-4 10 M6 12l8 3 1 6H3Z M7 16l-1 5M11 17v4 M19 3v6M16 6h6",
  elevator: "M5 3h14v18H5Z M9 9l3-3 3 3M9 15l3 3 3-3",
  light: "M9 17h6M9 21h6 M8 14a6 6 0 1 1 8 0l-1 3H9Z",
  calendar: "M4 5h16v16H4ZM8 2v6M16 2v6M4 10h16",
  check: "m5 12 4 4L19 6",
  issue: "M12 3 2 21h20ZM12 9v5M12 17v1",
} as const;

export default function Icon({
  name,
  size = 20,
}: {
  name: keyof typeof paths;
  size?: number;
}) {
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
      <path d={paths[name]} />
    </svg>
  );
}
