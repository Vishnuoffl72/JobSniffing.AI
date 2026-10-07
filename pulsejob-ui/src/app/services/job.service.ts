import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { JobSearchRequest, JobSearchResult } from '../models/job.model';

@Injectable({
  providedIn: 'root'
})
export class JobService {
  private readonly http = inject(HttpClient);
  // Default ASP.NET Core API dev URL
  private readonly apiUrl = 'http://localhost:5247/api/jobs';

  searchJobs(request: JobSearchRequest): Observable<JobSearchResult[]> {
    return this.http.post<JobSearchResult[]>(`${this.apiUrl}/search`, request);
  }

  getJobsByDate(date?: string, limit: number = 100): Observable<JobSearchResult[]> {
    const url = date ? `${this.apiUrl}?date=${encodeURIComponent(date)}&limit=${limit}` : `${this.apiUrl}?limit=${limit}`;
    return this.http.get<JobSearchResult[]>(url);
  }

  getAvailableDates(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/dates`);
  }

  toggleApplied(id: number, isApplied: boolean): Observable<JobSearchResult> {
    return this.http.patch<JobSearchResult>(`${this.apiUrl}/${id}/applied`, { isApplied });
  }
}

