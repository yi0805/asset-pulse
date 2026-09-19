import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, forkJoin, of, switchMap } from 'rxjs';
import { ApiError, mapApiError } from '../../shared/api-error';
import { EmptyState } from '../../shared/states/empty-state';
import { ErrorState } from '../../shared/states/error-state';
import { LoadingState } from '../../shared/states/loading-state';
import { Asset, AssetEvent, PagedResponse } from '../../models/api.models';
import { AssetApiService } from '../asset-api.service';

@Component({
  selector: 'app-asset-detail-page',
  imports: [RouterLink, DatePipe, LoadingState, ErrorState, EmptyState],
  templateUrl: './asset-detail-page.html',
  styleUrl: './asset-detail-page.css',
})
export class AssetDetailPage {
  private readonly api = inject(AssetApiService);
  private readonly route = inject(ActivatedRoute);
  readonly asset = signal<Asset | null>(null);
  readonly events = signal<PagedResponse<AssetEvent> | null>(null);
  readonly error = signal<ApiError | null>(null);
  readonly loading = signal(true);
  private id = 0;

  constructor() {
    this.route.paramMap
      .pipe(
        switchMap((params) => {
          this.id = Number(params.get('id'));
          return this.load();
        }),
      )
      .subscribe();
  }

  retry(): void {
    this.load().subscribe();
  }
  changeHistoryPage(page: number): void {
    this.load(page).subscribe();
  }
  measurement(value: number | null, unit: string): string {
    return value === null ? 'Unavailable' : `${value} ${unit}`;
  }
  transition(event: AssetEvent): string {
    return `${event.previousStatus ?? 'Initial'} → ${event.newStatus}`;
  }

  private load(page = this.events()?.page ?? 1) {
    this.loading.set(true);
    this.error.set(null);
    return forkJoin({
      asset: this.api.getAsset(this.id),
      events: this.api.getEvents(this.id, { page }),
    }).pipe(
      catchError((response) => {
        this.error.set(mapApiError(response));
        this.loading.set(false);
        return of(null);
      }),
      switchMap((result) => {
        if (result) {
          this.asset.set(result.asset);
          this.events.set(result.events);
        }
        this.loading.set(false);
        return of(result);
      }),
    );
  }
}
