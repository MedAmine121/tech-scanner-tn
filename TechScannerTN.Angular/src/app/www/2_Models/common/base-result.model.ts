import { ResultType } from './result-type.model';

export interface BaseResult<T> {
  model: T | null;
  message: string | null;
  resultType: ResultType;
}
