export interface JobSearchResult {
  id: number;
  scrapeTimestamp: string;
  jobTitle: string;
  company: string;
  platform: 'LinkedIn' | 'Naukri' | 'Instahyre' | string;
  directLink: string;
  matchScore: number;
  matchReason: string;
  postedDate: string;
  isApplied: boolean;
}

export interface JobSearchRequest {
  jobTitle: string;
  location: string;
  postedWithinHours: number;
  resumeText: string;
}

