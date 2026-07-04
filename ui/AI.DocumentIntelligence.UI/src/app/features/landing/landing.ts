import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

interface FeatureCard {
  readonly icon: string;
  readonly title: string;
  readonly description: string;
  readonly link: string;
  readonly cta: string;
}

/**
 * Public marketing/landing page. Copy must match the product spec verbatim
 * (README.MD §Landing Page, lines 124-150).
 */
@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatCardModule, MatIconModule],
  templateUrl: './landing.html',
  styleUrl: './landing.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Landing {
  protected readonly title = 'AI Document Intelligence Platform';
  protected readonly subtitle =
    'Analyze, compare, and understand complex documents using advanced AI reasoning and enterprise-grade document intelligence.';

  protected readonly features: readonly FeatureCard[] = [
    {
      icon: 'insights',
      title: 'Analysis Mode',
      description: 'Analyze documents and extract meaningful insights using AI.',
      link: '/analysis',
      cta: 'Start Analysis',
    },
    {
      icon: 'compare_arrows',
      title: 'Comparison Mode',
      description: 'Compare multiple documents and identify key differences using intelligent analysis.',
      link: '/compare',
      cta: 'Start Comparison',
    },
  ];
}
