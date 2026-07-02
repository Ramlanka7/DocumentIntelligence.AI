import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

import { ActivityEventType, ActivityItem } from '../../models/admin-dashboard.models';

// ── Helpers ───────────────────────────────────────────────────────────────────

const TYPE_ICONS: Record<ActivityEventType, string> = {
  analysis: 'analytics',
  comparison: 'compare_arrows',
  chat: 'forum',
  login: 'login',
  upload: 'upload_file',
};

const TYPE_COLORS: Record<ActivityEventType, string> = {
  analysis: '#60a5fa',
  comparison: '#34d399',
  chat: '#a78bfa',
  login: '#fbbf24',
  upload: '#f472b6',
};

function timeAgo(iso: string): string {
  const diffMs = Date.now() - new Date(iso).getTime();
  const minutes = Math.floor(diffMs / 60_000);
  if (minutes < 1) return 'just now';
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(diffMs / 3_600_000);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(diffMs / 86_400_000);
  return `${days}d ago`;
}

export interface EnrichedActivity {
  readonly id: string;
  readonly type: ActivityEventType;
  readonly icon: string;
  readonly iconColor: string;
  readonly userEmail: string;
  readonly description: string;
  readonly timeAgo: string;
}

/**
 * Presentational component rendering the recent-activity feed.
 * Shows icon, user email, description, and relative timestamp for each item.
 */
@Component({
  selector: 'app-activity-feed',
  standalone: true,
  imports: [MatIconModule],
  templateUrl: './activity-feed.html',
  styleUrl: './activity-feed.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActivityFeedComponent {
  readonly activities = input<readonly ActivityItem[]>([]);
  readonly loading = input(false);

  protected readonly enriched = computed<readonly EnrichedActivity[]>(() =>
    this.activities().map((a) => ({
      id: a.id,
      type: a.type,
      icon: TYPE_ICONS[a.type],
      iconColor: TYPE_COLORS[a.type],
      userEmail: a.userEmail,
      description: a.description,
      timeAgo: timeAgo(a.timestamp),
    })),
  );
}
