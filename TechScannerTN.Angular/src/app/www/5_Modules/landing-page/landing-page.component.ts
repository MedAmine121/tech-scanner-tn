import { Component, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { BaseComponent } from '../shared/base-component/base-component';
import { NavBarComponent } from '../shared/nav-bar-component/nav-bar-component';
import { FooterComponent } from '../shared/footer-component/footer-component';

interface DealComparison {
  id: number;
  skuCode: string;
  title: string;
  category: string;
  image: string;
  bestPrice: number;
  bestStore: string;
  prices: { store: string; price: number; isBest: boolean }[];
  savings: number;
}

interface CanonicalTapeItem {
  id: number;
  brand: string;
  category: string;
  title: string;
  isAllTimeLow: boolean;
  offers: { store: string; price: number; discountLabel?: string; isLowest: boolean }[];
  sparklinePoints: string;
  cheapestPrice: number;
  highestPrice: number;
  savingsAmount: number;
}

interface IspPlanComparison {
  provider: string;
  planName: string;
  speed: string;
  price: number;
  type: string;
  featured?: boolean;
}

@Component({
  selector: 'app-landing-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, NavBarComponent, FooterComponent],
  templateUrl: './landing-page.component.html',
  styleUrl: './landing-page.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LandingPageComponent extends BaseComponent {
  readonly isAuth = computed<boolean>(() => this.userService.isAuthenticated());

  readonly heroSearchQuery = signal<string>('RTX 4060 | IdeaPad Slim 3 | 82RK009CFE');
  readonly activeCategoryTab = signal<string>('all');

  readonly dealCards = signal<DealComparison[]>([
    {
      id: 1,
      skuCode: 'LAPTOP // ROG-G16',
      title: 'ASUS ROG Strix G16 (i7 14th Gen / RTX 4060 / 16GB / 1TB SSD)',
      category: 'laptops',
      image: '💻',
      bestPrice: 4199,
      bestStore: 'TunisiaNet',
      prices: [
        { store: 'TunisiaNet', price: 4199, isBest: true },
        { store: 'MyTek', price: 4499, isBest: false },
        { store: 'SpaceNet', price: 4549, isBest: false }
      ],
      savings: 350
    },
    {
      id: 2,
      skuCode: 'GPU // MSI-4070TIS',
      title: 'MSI GeForce RTX 4070 Ti SUPER Gaming X Slim 16G',
      category: 'components',
      image: '🎮',
      bestPrice: 3249,
      bestStore: 'MyTek',
      prices: [
        { store: 'MyTek', price: 3249, isBest: true },
        { store: 'SpaceNet', price: 3499, isBest: false },
        { store: 'TunisiaNet', price: 3590, isBest: false }
      ],
      savings: 341
    },
    {
      id: 3,
      skuCode: 'LAPTOP // MBA-M3-13',
      title: 'Apple MacBook Air 13" M3 (8-Core CPU / 10-Core GPU / 512GB)',
      category: 'laptops',
      image: '⚡',
      bestPrice: 3899,
      bestStore: 'SpaceNet',
      prices: [
        { store: 'SpaceNet', price: 3899, isBest: true },
        { store: 'TunisiaNet', price: 4099, isBest: false },
        { store: 'MyTek', price: 4199, isBest: false }
      ],
      savings: 300
    },
    {
      id: 4,
      skuCode: 'MONITOR // SAM-G5-27',
      title: 'Samsung 27" Odyssey G5 QHD 165Hz 1ms Curved Gaming Monitor',
      category: 'peripherals',
      image: '🖥️',
      bestPrice: 849,
      bestStore: 'MyTek',
      prices: [
        { store: 'MyTek', price: 849, isBest: true },
        { store: 'TunisiaNet', price: 929, isBest: false },
        { store: 'SpaceNet', price: 949, isBest: false }
      ],
      savings: 100
    }
  ]);

  readonly canonicalTapePreview = signal<CanonicalTapeItem[]>([
    {
      id: 1,
      brand: 'HP',
      category: 'LAPTOPS',
      title: 'HP Victus 15 (Ryzen 5, RTX 2050, 16GB)',
      isAllTimeLow: true,
      offers: [
        { store: 'MyTek', price: 2679, discountLabel: '-7% · IN STOCK', isLowest: true },
        { store: 'TunisiaNet', price: 2799, isLowest: false },
        { store: 'SpaceNet', price: 2889, isLowest: false }
      ],
      sparklinePoints: '2,4 22,8 42,7 64,14 82,19 94,21',
      cheapestPrice: 2679,
      highestPrice: 2889,
      savingsAmount: 210
    },
    {
      id: 2,
      brand: 'MSI',
      category: 'GPUS',
      title: 'MSI GeForce RTX 4060 Ventus 2X 8GB',
      isAllTimeLow: true,
      offers: [
        { store: 'MyTek', price: 1289, discountLabel: '-11% · IN STOCK', isLowest: true },
        { store: 'SpaceNet', price: 1379, isLowest: false },
        { store: 'TunisiaNet', price: 1449, isLowest: false }
      ],
      sparklinePoints: '2,5 20,6 44,12 66,11 80,18 94,21',
      cheapestPrice: 1289,
      highestPrice: 1449,
      savingsAmount: 160
    }
  ]);

  readonly ispPlans = signal<IspPlanComparison[]>([
    {
      provider: 'TOPNET',
      planName: 'SMART RAPIDO FIBRE',
      speed: '50 Mbps',
      price: 59.9,
      type: 'FTTH Fiber',
      featured: true
    },
    {
      provider: 'TUNISIE TELECOM',
      planName: 'WICI DUO Pro',
      speed: '20 Mbps',
      price: 43.5,
      type: 'VDSL High Speed',
      featured: false
    },
    {
      provider: 'ORANGE TN',
      planName: 'Livebox Fibre Optique',
      speed: '100 Mbps',
      price: 94.0,
      type: 'Ultra Fiber',
      featured: false
    },
    {
      provider: 'OOREDOO TN',
      planName: 'Ooredoo Fibre Home',
      speed: '50 Mbps',
      price: 58.0,
      type: 'FTTH Fiber',
      featured: false
    }
  ]);

  readonly filteredDeals = computed<DealComparison[]>(() => {
    const tab = this.activeCategoryTab();
    const deals = this.dealCards();
    if (tab === 'all') return deals;
    return deals.filter(d => d.category === tab);
  });

  setCategoryTab(category: string): void {
    this.activeCategoryTab.set(category);
  }

  onHeroSearch(): void {
    this.redirectTo(['/products']);
  }
}
