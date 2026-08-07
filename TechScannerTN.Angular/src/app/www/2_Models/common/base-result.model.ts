export interface BaseResult<T> {
    success: boolean;
    data?: T;
    message?: string;
    errors?: any;
}