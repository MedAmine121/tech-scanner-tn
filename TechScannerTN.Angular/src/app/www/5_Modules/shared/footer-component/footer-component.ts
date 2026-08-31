import { Component, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './footer-component.html',
  styleUrl: './footer-component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FooterComponent {
  readonly currentYear = signal<number>(new Date().getFullYear());
}

