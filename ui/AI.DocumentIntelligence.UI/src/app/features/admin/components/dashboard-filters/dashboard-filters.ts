import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
  input,
  output,
} from '@angular/core';
import { ReactiveFormsModule, FormGroup, FormControl } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { Subscription, debounceTime } from 'rxjs';

import { DashboardFilters, OperationFilter } from '../../models/admin-dashboard.models';

interface FiltersForm {
  dateFrom: FormControl<string>;
  dateTo: FormControl<string>;
  operationType: FormControl<OperationFilter>;
  userId: FormControl<string>;
}

/**
 * Presentational filter bar for the Admin Dashboard.
 * Emits `filtersChange` debounced whenever any field changes.
 * Uses native `<input type="date">` inside Material form fields to avoid
 * requiring the `@angular/animations` peer-dependency for the calendar popup.
 */
@Component({
  selector: 'app-dashboard-filters',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatIconModule,
    MatButtonModule,
  ],
  templateUrl: './dashboard-filters.html',
  styleUrl: './dashboard-filters.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardFiltersComponent implements OnInit, OnDestroy {
  readonly currentFilters = input<DashboardFilters>({
    dateFrom: null,
    dateTo: null,
    operationType: 'all',
    userId: '',
  });

  readonly filtersChange = output<DashboardFilters>();

  protected readonly filterForm = new FormGroup<FiltersForm>({
    dateFrom: new FormControl<string>('', { nonNullable: true }),
    dateTo: new FormControl<string>('', { nonNullable: true }),
    operationType: new FormControl<OperationFilter>('all', { nonNullable: true }),
    userId: new FormControl<string>('', { nonNullable: true }),
  });

  private sub?: Subscription;

  readonly operationTypes: ReadonlyArray<{ value: OperationFilter; label: string }> = [
    { value: 'all', label: 'All Operations' },
    { value: 'analysis', label: 'Analysis' },
    { value: 'comparison', label: 'Comparison' },
    { value: 'chat', label: 'Chat' },
  ];

  ngOnInit(): void {
    // Sync initial value from parent
    const f = this.currentFilters();
    this.filterForm.setValue({
      dateFrom: f.dateFrom ?? '',
      dateTo: f.dateTo ?? '',
      operationType: f.operationType,
      userId: f.userId,
    });

    this.sub = this.filterForm.valueChanges.pipe(debounceTime(300)).subscribe((val) => {
      this.filtersChange.emit({
        dateFrom: val.dateFrom?.trim() || null,
        dateTo: val.dateTo?.trim() || null,
        operationType: val.operationType ?? 'all',
        userId: val.userId?.trim() ?? '',
      });
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  protected resetFilters(): void {
    this.filterForm.reset({
      dateFrom: '',
      dateTo: '',
      operationType: 'all',
      userId: '',
    });
  }
}
