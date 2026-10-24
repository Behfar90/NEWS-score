import type { MeasurementType } from "@/lib/types";
import styles from "./MeasurementField.module.css";

type MeasurementFieldProps = {
  name: MeasurementType;
  label: string;
  helperText: string;
};

export function MeasurementField({
  name,
  label,
  helperText,
}: MeasurementFieldProps) {
  const inputId = `measurement-${name}`;
  const helperId = `${inputId}-helper`;

  return (
    <div className={styles.field}>
      <div className={styles.labelGroup}>
        <label htmlFor={inputId} className={styles.label}>
          {label}
        </label>
        <p id={helperId} className={styles.helperText}>
          {helperText}
        </p>
      </div>
      <input
        id={inputId}
        name={name}
        className={styles.input}
        type="text"
        inputMode="numeric"
        pattern="-?[0-9]+"
        title="Enter only an integer number"
        autoComplete="off"
        required
        aria-describedby={helperId}
      />
    </div>
  );
}
