import { DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
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
  asset: Asset | null = null;
  events: PagedResponse<AssetEvent> | null = null;
  error: ApiError | null = null;
  loading = true;
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

  private load(page = this.events?.page ?? 1) {
    this.loading = true;
    this.error = null;
    return forkJoin({
      asset: this.api.getAsset(this.id),
      events: this.api.getEvents(this.id, { page }),
    }).pipe(
      catchError((response) => {
        this.error = mapApiError(response);
        this.loading = false;
        return of(null);
      }),
      switchMap((result) => {
        if (result) {
          this.asset = result.asset;
          this.events = result.events;
        }
        this.loading = false;
        return of(result);
      }),
    );
  }
}
