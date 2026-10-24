import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "NEWS Score",
  description: "Calculate a simplified NEWS score from vital signs.",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
