import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Alarm, AlarmListQuery, PagedResponse } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class AlarmApiService {
  private readonly http = inject(HttpClient);

  getAlarms(query: AlarmListQuery = {}): Observable<PagedResponse<Alarm>> {
    return this.http.get<PagedResponse<Alarm>>('/api/alarms', {
      params: buildParams(query),
    });
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
