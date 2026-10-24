import styles from "./ScoreResult.module.css";

type ScoreResultProps = {
  score: number;
};

export function ScoreResult({ score }: ScoreResultProps) {
  return (
    <output className={styles.result}>
      News score: <strong className={styles.score}>{score}</strong>
    </output>
  );
}
