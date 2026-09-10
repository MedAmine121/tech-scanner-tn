import { Injectable, inject, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { ProductDALService } from '../3_DAL/product-dal.service';
import {
  ProductComparisonDto,
  ProductFilterRequest,
  PriceHistoryDto,
  MarketOverviewDto
} from '../2_Models/products/product-comparison.model';

@Injectable({
  providedIn: 'root'
})
export class ProductBLLService {
  private dal = inject(ProductDALService);

  // State signals
  readonly products = signal<ProductComparisonDto[]>([]);
  readonly totalCount = signal<number>(0);
  readonly page = signal<number>(1);
  readonly pageSize = signal<number>(12);
  readonly totalPages = signal<number>(0);
  readonly loading = signal<boolean>(false);

  // Selected product modal state
  readonly selectedProduct = signal<ProductComparisonDto | null>(null);
  readonly selectedPriceHistory = signal<PriceHistoryDto[]>([]);
  readonly loadingHistory = signal<boolean>(false);

  // Market overview stats
  readonly marketOverview = signal<MarketOverviewDto | null>(null);

  // Active filters
  readonly searchTerm = signal<string>('');
  readonly selectedRetailerId = signal<number | null>(null);
  readonly onlyInStock = signal<boolean>(false);
  readonly onlyHistoricalLows = signal<boolean>(false);
  readonly minPrice = signal<number | null>(null);
  readonly maxPrice = signal<number | null>(null);
  readonly sortBy = signal<string>('cheapest');

  loadProducts(targetPage: number = 1): void {
    this.page.set(targetPage);
    this.loading.set(true);

    const filter: ProductFilterRequest = {
      page: this.page(),
      pageSize: this.pageSize(),
      searchTerm: this.searchTerm() || undefined,
      retailerId: this.selectedRetailerId() ?? undefined,
      onlyInStock: this.onlyInStock() || undefined,
      onlyHistoricalLows: this.onlyHistoricalLows() || undefined,
      minPrice: this.minPrice() ?? undefined,
      maxPrice: this.maxPrice() ?? undefined,
      sortBy: this.sortBy()
    };

    this.dal.getProducts$(filter)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: result => {
          this.products.set(result.items || []);
          this.totalCount.set(result.totalCount || 0);
          this.totalPages.set(result.totalPages || 0);
        },
        error: err => {
          console.error('Error fetching products:', err);
          this.products.set([]);
          this.totalCount.set(0);
        }
      });
  }

  loadMarketOverview(): void {
    this.dal.getMarketOverview$().subscribe({
      next: overview => this.marketOverview.set(overview),
      error: err => console.warn('Could not load market overview:', err)
    });
  }

  selectProduct(product: ProductComparisonDto): void {
    this.selectedProduct.set(product);
    this.loadingHistory.set(true);
    this.selectedPriceHistory.set([]);

    this.dal.getPriceHistory$(product.id)
      .pipe(finalize(() => this.loadingHistory.set(false)))
      .subscribe({
        next: history => this.selectedPriceHistory.set(history || []),
        error: err => {
          console.error('Error fetching price history:', err);
          this.selectedPriceHistory.set([]);
        }
      });
  }

  closeSelectedProduct(): void {
    this.selectedProduct.set(null);
    this.selectedPriceHistory.set([]);
  }

  setSearch(term: string): void {
    this.searchTerm.set(term);
    this.loadProducts(1);
  }

  setRetailer(retailerId: number | null): void {
    this.selectedRetailerId.set(retailerId);
    this.loadProducts(1);
  }

  setSortBy(sortBy: string): void {
    this.sortBy.set(sortBy);
    this.loadProducts(1);
  }

  toggleInStock(): void {
    this.onlyInStock.set(!this.onlyInStock());
    this.loadProducts(1);
  }

  toggleHistoricalLows(): void {
    this.onlyHistoricalLows.set(!this.onlyHistoricalLows());
    this.loadProducts(1);
  }

  setPriceRange(min: number | null, max: number | null): void {
    this.minPrice.set(min);
    this.maxPrice.set(max);
    this.loadProducts(1);
  }

  resetFilters(): void {
    this.searchTerm.set('');
    this.selectedRetailerId.set(null);
    this.onlyInStock.set(false);
    this.onlyHistoricalLows.set(false);
    this.minPrice.set(null);
    this.maxPrice.set(null);
    this.sortBy.set('cheapest');
    this.loadProducts(1);
  }
}

