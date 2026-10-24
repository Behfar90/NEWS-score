"use client";

import type { MeasurementType } from "@/lib/types";
import { MeasurementField } from "./MeasurementField";
import styles from "./NewsForm.module.css";

const FIELDS: { name: MeasurementType; label: string; helperText: string }[] = [
  { name: "TEMP", label: "Body temperature", helperText: "Degrees celcius" },
  { name: "HR", label: "Heartrate", helperText: "Beats per minute" },
  { name: "RR", label: "Respiratory rate", helperText: "Breaths per minute" },
];

export function NewsForm() {
  function handleSubmit(event: React.SubmitEvent<HTMLFormElement>) {
    event.preventDefault();
  }

  return (
    <div className={styles.container}>
      <h1 className={styles.title}>NEWS score calculator</h1>
      <form className={styles.form} onSubmit={handleSubmit}>
        {FIELDS.map((field) => (
          <MeasurementField key={field.name} {...field} />
        ))}
        <div className={styles.actions}>
          <button type="submit" className={styles.primaryButton}>
            Calculate NEWS score
          </button>
          <button type="reset" className={styles.secondaryButton}>
            Reset form
          </button>
        </div>
      </form>
    </div>
  );
}
