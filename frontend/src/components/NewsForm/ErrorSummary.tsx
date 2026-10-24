import type { FieldErrors, MeasurementType } from "@/lib/types";
import { measurementInputId } from "./MeasurementField";
import styles from "./ErrorSummary.module.css";

type ErrorSummaryProps = {
  fieldErrors?: FieldErrors;
};

export function ErrorSummary({ fieldErrors }: ErrorSummaryProps) {
  const entries = Object.entries(fieldErrors ?? {}) as [MeasurementType, string][];

  return (
    <div role="alert" className={styles.errorSummary}>
      <p className={styles.title}>Could not calculate the NEWS score.</p>
      {entries.length > 0 ? (
        <ul className={styles.list}>
          {entries.map(([type, message]) => (
            <li key={type}>
              <a href={`#${measurementInputId(type)}`} className={styles.link}>
                {message}
              </a>
            </li>
          ))}
        </ul>
      ) : (
        <p>Please try again.</p>
      )}
    </div>
  );
}
