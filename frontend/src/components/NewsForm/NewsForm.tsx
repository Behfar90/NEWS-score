"use client";

import { useState } from "react";
import { calculateNewsScore, type CalculateResult } from "@/lib/newsApi";
import type { MeasurementType } from "@/lib/types";
import { ErrorSummary } from "./ErrorSummary";
import { MeasurementField } from "./MeasurementField";
import { ScoreResult } from "./ScoreResult";
import styles from "./NewsForm.module.css";

const FIELDS: { name: MeasurementType; label: string; helperText: string }[] = [
  { name: "TEMP", label: "Body temperature", helperText: "Degrees celcius" },
  { name: "HR", label: "Heartrate", helperText: "Beats per minute" },
  { name: "RR", label: "Respiratory rate", helperText: "Breaths per minute" },
];

export function NewsForm() {
  const [result, setResult] = useState<CalculateResult | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [editedFields, setEditedFields] = useState<MeasurementType[]>([]);

  async function handleSubmit(event: React.SubmitEvent<HTMLFormElement>) {
    event.preventDefault();
    if (isSubmitting) {
      return;
    }

    const formData = new FormData(event.currentTarget);

    setResult(null);
    setEditedFields([]);
    setIsSubmitting(true);
    const calculated = await calculateNewsScore({
      measurements: FIELDS.map((field) => ({
        type: field.name,
        value: Number(formData.get(field.name)),
      })),
    });
    setResult(calculated);
    setIsSubmitting(false);
  }

  function handleInput(event: React.InputEvent<HTMLFormElement>) {
    if (result?.kind === "success") {
      setResult(null);
      return;
    }

    const name = (event.target as HTMLInputElement).name as MeasurementType;
    if (!editedFields.includes(name)) {
      setEditedFields([...editedFields, name]);
    }
  }

  function handleReset() {
    setResult(null);
    setEditedFields([]);
  }

  const fieldErrors = result?.kind === "invalid" ? result.fieldErrors : {};

  return (
    <div className={styles.container}>
      <h1 className={styles.title}>NEWS score calculator</h1>
      <form
        className={styles.form}
        onSubmit={handleSubmit}
        onInput={handleInput}
        onReset={handleReset}
      >
        {FIELDS.map((field) => (
          <MeasurementField
            key={field.name}
            {...field}
            invalid={field.name in fieldErrors && !editedFields.includes(field.name)}
          />
        ))}
        <div className={styles.actions}>
          <button
            type="submit"
            className={styles.primaryButton}
            aria-disabled={isSubmitting || undefined}
          >
            Calculate NEWS score
          </button>
          <button type="reset" className={styles.secondaryButton}>
            Reset form
          </button>
        </div>
        {result?.kind === "invalid" && <ErrorSummary fieldErrors={result.fieldErrors} />}
        {result?.kind === "failed" && <ErrorSummary />}
        <ScoreResult score={result?.kind === "success" ? result.score : null} />
      </form>
    </div>
  );
}
