import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  Asset,
  AssetEvent,
  AssetEventListQuery,
  AssetListQuery,
  AssetUpsertRequest,
  PagedResponse,
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class AssetApiService {
  private readonly http = inject(HttpClient);

  getAssets(query: AssetListQuery = {}): Observable<PagedResponse<Asset>> {
    return this.http.get<PagedResponse<Asset>>('/api/assets', {
      params: buildParams(query),
    });
  }

  getAsset(id: number): Observable<Asset> {
    return this.http.get<Asset>(`/api/assets/${id}`);
  }

  getEvents(id: number, query: AssetEventListQuery = {}): Observable<PagedResponse<AssetEvent>> {
    return this.http.get<PagedResponse<AssetEvent>>(`/api/assets/${id}/events`, {
      params: buildParams(query),
    });
  }

  createAsset(request: AssetUpsertRequest): Observable<Asset> {
    return this.http.post<Asset>('/api/assets', request);
  }

  updateAsset(id: number, request: AssetUpsertRequest): Observable<Asset> {
    return this.http.put<Asset>(`/api/assets/${id}`, request);
  }
}

function buildParams(query: object): HttpParams {
  let params = new HttpParams();

  for (const [key, value] of Object.entries(query)) {
    if (value !== undefined) {
      params = params.set(key, String(value));
    }
  }

  return params;
}
