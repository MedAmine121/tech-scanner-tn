import { Component, OnInit, inject, ChangeDetectionStrategy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { NavBarComponent } from '../../shared/nav-bar-component/nav-bar-component';
import { FooterComponent } from '../../shared/footer-component/footer-component';
import { ProductBLLService } from '../../../4_BLL/product-bll.service';
import { ProductComparisonDto, RetailerOfferDto } from '../../../2_Models/products/product-comparison.model';

@Component({
  selector: 'app-products-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, NavBarComponent, FooterComponent],
  templateUrl: './products-list.component.html',
  styleUrl: './products-list.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProductsListComponent implements OnInit {
  readonly bll = inject(ProductBLLService);

  readonly searchInput = signal<string>('');
  readonly minPriceInput = signal<number | null>(null);
  readonly maxPriceInput = signal<number | null>(null);

  ngOnInit(): void {
    this.bll.loadProducts(1);
    this.bll.loadMarketOverview();
  }

  onSearchSubmit(): void {
    this.bll.setSearch(this.searchInput());
  }

  onSearchClear(): void {
    this.searchInput.set('');
    this.bll.setSearch('');
  }

  onRetailerFilter(retailerId: number | null): void {
    this.bll.setRetailer(retailerId);
  }

  onSortChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.bll.setSortBy(select.value);
  }

  onApplyPriceFilter(): void {
    this.bll.setPriceRange(this.minPriceInput(), this.maxPriceInput());
  }

  onPageChange(targetPage: number): void {
    if (targetPage >= 1 && targetPage <= this.bll.totalPages()) {
      this.bll.loadProducts(targetPage);
      window.scrollTo({ top: 300, behavior: 'smooth' });
    }
  }

  getStoreBadgeClass(storeCode?: string): string {
    const code = storeCode?.toUpperCase() || '';
    if (code.includes('MYTEK')) return 'store-mytek';
    if (code.includes('TUNISIANET')) return 'store-tunisianet';
    if (code.includes('SPACENET')) return 'store-spacenet';
    return 'store-generic';
  }

  getStoreDisplayName(code?: string): string {
    const c = code?.toUpperCase() || '';
    if (c.includes('MYTEK')) return 'MyTek';
    if (c.includes('TUNISIANET')) return 'TunisiaNet';
    if (c.includes('SPACENET')) return 'SpaceNet';
    return code || 'Store';
  }

  getLowestOffer(product: ProductComparisonDto): RetailerOfferDto | undefined {
    return product.offers.find(o => o.isCheapest) || product.offers[0];
  }

  formatDate(dateStr?: string): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-TN', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' });
  }
}

