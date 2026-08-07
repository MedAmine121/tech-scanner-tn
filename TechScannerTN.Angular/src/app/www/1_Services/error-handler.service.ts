import { ErrorHandler, Injectable } from "@angular/core";
@Injectable({
    providedIn: 'root'
})
export class GlobalErrorHandler implements ErrorHandler{
    handleError(error: unknown): void {
        throw new Error("Method not implemented.");
    }
    normalize(error: unknown): string {
        return '';
    }
}