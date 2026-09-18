export type AssetStatus = 'Healthy' | 'Warning' | 'Critical';

export type AlarmSeverity = 'Warning' | 'Critical';

export type AlarmStatus = 'Active' | 'Acknowledged' | 'Resolved';

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface Asset {
  id: number;
  name: string;
  assetCode: string;
  type: string;
  location: string;
  temperature: number | null;
  pressure: number | null;
  lastUpdated: string;
  status: AssetStatus;
}

export interface AssetEvent {
  id: number;
  assetId: number;
  previousStatus: AssetStatus | null;
  newStatus: AssetStatus;
  timestamp: string;
  description: string;
}

export interface Alarm {
  id: number;
  assetId: number;
  assetCode: string;
  assetName: string;
  severity: AlarmSeverity;
  message: string;
  status: AlarmStatus;
  createdAt: string;
  acknowledgedAt: string | null;
  resolvedAt: string | null;
}

export interface AssetListQuery {
  search?: string;
  type?: string;
  location?: string;
  status?: AssetStatus;
  page?: number;
  pageSize?: number;
}

export interface AssetEventListQuery {
  page?: number;
  pageSize?: number;
}

export interface AlarmListQuery {
  assetId?: number;
  severity?: AlarmSeverity;
  status?: AlarmStatus;
  page?: number;
  pageSize?: number;
}
