export type MeasurementType = "TEMP" | "HR" | "RR";

export type NewsScoreRequest = {
  measurements: { type: MeasurementType; value: number }[];
};

export type NewsScoreResponse = {
  score: number;
};

export type ValidationProblem = {
  title: string;
  status: number;
  errors?: Record<string, string[]>;
};

export type FieldErrors = Partial<Record<MeasurementType, string>>;
