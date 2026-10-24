import styles from "./ScoreResult.module.css";

type ScoreResultProps = {
  score: number | null;
};

export function ScoreResult({ score }: ScoreResultProps) {
  return (
    <output aria-live="polite" aria-atomic="true">
      {score !== null && (
        <span className={styles.result}>
          News score: <strong className={styles.score}>{score}</strong>
        </span>
      )}
    </output>
  );
}
