import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../1_Services/api.service';
import {
  ProductComparisonDto,
  ProductFilterRequest,
  PagedResult,
  PriceHistoryDto,
  MarketOverviewDto
} from '../2_Models/products/product-comparison.model';

@Injectable({
  providedIn: 'root'
})
export class ProductDALService {
  private api = inject(ApiService);

  getProducts$(filter: ProductFilterRequest): Observable<PagedResult<ProductComparisonDto>> {
    return this.api.GetRaw$<PagedResult<ProductComparisonDto>>('api/products', filter);
  }

  getProductDetails$(id: number): Observable<ProductComparisonDto> {
    return this.api.GetRaw$<ProductComparisonDto>(`api/products/${id}`);
  }

  getPriceHistory$(id: number): Observable<PriceHistoryDto[]> {
    return this.api.GetRaw$<PriceHistoryDto[]>(`api/products/${id}/price-history`);
  }

  getPriceDrops$(limit: number = 20): Observable<ProductComparisonDto[]> {
    return this.api.GetRaw$<ProductComparisonDto[]>('api/products/price-drops', { limit });
  }

  getMarketOverview$(): Observable<MarketOverviewDto> {
    return this.api.GetRaw$<MarketOverviewDto>('api/analytics/overview');
  }
}

