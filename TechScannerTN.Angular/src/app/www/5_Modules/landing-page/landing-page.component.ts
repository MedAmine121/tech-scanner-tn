import { Component, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { BaseComponent } from '../shared/base-component/base-component';
import { NavBarComponent } from '../shared/nav-bar-component/nav-bar-component';
import { FooterComponent } from '../shared/footer-component/footer-component';

interface DealComparison {
  id: number;
  title: string;
  category: string;
  image: string;
  bestPrice: number;
  bestStore: string;
  prices: { store: string; price: number; isBest: boolean }[];
  savings: number;
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
  imports: [CommonModule, RouterModule, NavBarComponent, FooterComponent],
  templateUrl: './landing-page.component.html',
  styleUrl: './landing-page.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LandingPageComponent extends BaseComponent {
  readonly isAuth = computed<boolean>(() => this.userService.isAuthenticated());

  readonly activeCategoryTab = signal<string>('laptops');

  readonly dealCards = signal<DealComparison[]>([
    {
      id: 1,
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

  readonly ispPlans = signal<IspPlanComparison[]>([
    {
      provider: 'Topnet',
      planName: 'SMART RAPIDO FIBRE',
      speed: '50 Mbps',
      price: 59.9,
      type: 'FTTH Fiber',
      featured: true
    },
    {
      provider: 'Tunisie Telecom',
      planName: 'WICI DUO Pro',
      speed: '20 Mbps',
      price: 43.5,
      type: 'VDSL High Speed',
      featured: false
    },
    {
      provider: 'Orange TN',
      planName: 'Livebox Fibre Optique',
      speed: '100 Mbps',
      price: 94.0,
      type: 'Ultra Fiber',
      featured: false
    },
    {
      provider: 'Ooredoo TN',
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
}

