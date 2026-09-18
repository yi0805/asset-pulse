import { DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, of, switchMap } from 'rxjs';
import { Asset, AssetListQuery, AssetStatus, PagedResponse } from '../../models/api.models';
import { mapApiError, ApiError } from '../../shared/api-error';
import { EmptyState } from '../../shared/states/empty-state';
import { ErrorState } from '../../shared/states/error-state';
import { LoadingState } from '../../shared/states/loading-state';
import { AssetApiService } from '../asset-api.service';

@Component({
  selector: 'app-assets-page',
  imports: [ReactiveFormsModule, RouterLink, DatePipe, LoadingState, ErrorState, EmptyState],
  templateUrl: './assets-page.html',
  styleUrl: './assets-page.css',
})
export class AssetsPage {
  private readonly api = inject(AssetApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);
  readonly filters = this.formBuilder.group({ search: '', type: '', location: '', status: '' });
  assets: PagedResponse<Asset> | null = null;
  error: ApiError | null = null;
  loading = true;
  readonly Math = Math;
  private query: AssetListQuery = {};

  constructor() {
    this.route.queryParamMap
      .pipe(
        switchMap((params) => {
          this.query = {
            search: params.get('search') || undefined,
            type: params.get('type') || undefined,
            location: params.get('location') || undefined,
            status: (params.get('status') as AssetStatus | null) || undefined,
            page: numberParam(params.get('page')),
            pageSize: numberParam(params.get('pageSize')),
          };
          this.filters.patchValue(
            {
              search: this.query.search ?? '',
              type: this.query.type ?? '',
              location: this.query.location ?? '',
              status: this.query.status ?? '',
            },
            { emitEvent: false },
          );
          return this.fetch(this.query);
        }),
      )
      .subscribe();
  }

  apply(): void {
    const value = this.filters.getRawValue();
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        search: trimOrUndefined(value.search),
        type: trimOrUndefined(value.type),
        location: trimOrUndefined(value.location),
        status: value.status || undefined,
        page: undefined,
        pageSize: this.query.pageSize || undefined,
      },
    });
  }

  reset(): void {
    this.filters.reset({ search: '', type: '', location: '', status: '' });
    this.apply();
  }

  changePage(page: number): void {
    void this.router.navigate([], { relativeTo: this.route, queryParams: { ...this.query, page } });
  }

  retry(): void {
    this.fetch(this.query).subscribe();
  }

  queryHasFilters(): boolean {
    return Boolean(
      this.query.search || this.query.type || this.query.location || this.query.status,
    );
  }

  measurement(value: number | null, unit: string): string {
    return value === null ? 'Unavailable' : `${value} ${unit}`;
  }

  private fetch(query: AssetListQuery) {
    this.loading = true;
    this.error = null;
    return this.api
      .getAssets(query)
      .pipe(
        catchError((response) => {
          this.error = mapApiError(response);
          this.loading = false;
          return of(null);
        }),
      )
      .pipe(
        switchMap((result) => {
          this.assets = result;
          this.loading = false;
          return of(result);
        }),
      );
  }
}

function numberParam(value: string | null): number | undefined {
  const number = Number(value);
  return Number.isInteger(number) && number > 0 ? number : undefined;
}
function trimOrUndefined(value: string | null): string | undefined {
  const trimmed = value?.trim();
  return trimmed || undefined;
}
