import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { JobService } from './services/job.service';
import { JobSearchResult } from './models/job.model';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  private fb = inject(FormBuilder);
  private jobService = inject(JobService);

  // Active view: 'search' (current instance search) vs 'history' (search history by date)
  activeTab: 'search' | 'history' = 'search';

  searchForm: FormGroup = this.fb.group({
    jobTitle: ['Senior Full Stack Engineer (.NET / Angular)', [Validators.required]],
    location: ['Bangalore / Remote', [Validators.required]],
    postedWithinHours: [24, [Validators.required]],
    resumeText: [
`SENIOR FULL STACK SOFTWARE ENGINEER
Key Skills: C#, .NET 8/9, ASP.NET Core Web API, Entity Framework Core, SQLite/PostgreSQL, Angular, TypeScript, Tailwind CSS, Docker, Microservices, REST APIs, Distributed Caching.
Experience: 6+ years building high-performance scalable web systems and cloud applications. Experienced in architecting robust REST endpoints, optimizing database queries, and designing responsive enterprise dashboards.`,
      [Validators.required]
    ]
  });

  // Current instance search state (Only displays results from the current search run)
  isLoading = false;
  currentSearchJobs: JobSearchResult[] = [];
  hasSearchedCurrentSession = false;
  errorMessage: string | null = null;
  activeBatchTime: string | null = null;
  selectedPlatformFilter: string = 'All';

  // History page state
  availableDates: string[] = [];
  selectedHistoryDate: string = '';
  historyJobs: JobSearchResult[] = [];
  isHistoryLoading = false;
  historyPlatformFilter: string = 'All';

  ngOnInit(): void {
    this.loadAvailableDates();
  }

  loadAvailableDates(): void {
    this.jobService.getAvailableDates().subscribe({
      next: (dates) => {
        this.availableDates = dates;
        if (dates.length > 0 && !this.selectedHistoryDate) {
          this.selectedHistoryDate = dates[0];
          this.loadHistoryForDate(this.selectedHistoryDate);
        }
      },
      error: (err) => {
        console.error('Error fetching available dates:', err);
      }
    });
  }

  loadHistoryForDate(dateStr: string): void {
    if (!dateStr) {
      this.historyJobs = [];
      return;
    }
    this.isHistoryLoading = true;
    this.selectedHistoryDate = dateStr;
    this.jobService.getJobsByDate(dateStr).subscribe({
      next: (results) => {
        this.historyJobs = results;
        this.isHistoryLoading = false;
      },
      error: (err) => {
        console.error('Error fetching jobs for date:', err);
        this.isHistoryLoading = false;
      }
    });
  }

  onDateChange(event: Event): void {
    const target = event.target as HTMLSelectElement;
    if (target && target.value) {
      this.loadHistoryForDate(target.value);
    }
  }

  switchTab(tab: 'search' | 'history'): void {
    this.activeTab = tab;
    if (tab === 'history') {
      this.loadAvailableDates();
      if (this.selectedHistoryDate) {
        this.loadHistoryForDate(this.selectedHistoryDate);
      }
    }
  }

  onSubmit(): void {
    if (this.searchForm.invalid) {
      this.searchForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = null;

    const req = this.searchForm.value;
    req.postedWithinHours = Number(req.postedWithinHours);

    this.jobService.searchJobs(req).subscribe({
      next: (results) => {
        this.currentSearchJobs = results;
        this.hasSearchedCurrentSession = true;
        if (results.length > 0) {
          this.activeBatchTime = results[0].scrapeTimestamp;
        }
        this.isLoading = false;
        // Refresh available dates in background for the history page
        this.loadAvailableDates();
      },
      error: (err) => {
        console.error('Search failed:', err);
        this.errorMessage = 'Failed to execute job scraping & AI matching pipeline. Ensure the backend API is running.';
        this.isLoading = false;
      }
    });
  }

  toggleApplied(job: JobSearchResult): void {
    const updatedStatus = !job.isApplied;
    this.jobService.toggleApplied(job.id, updatedStatus).subscribe({
      next: (res) => {
        job.isApplied = res.isApplied;
      },
      error: (err) => {
        console.error('Could not toggle application status:', err);
      }
    });
  }

  get filteredCurrentJobs(): JobSearchResult[] {
    if (this.selectedPlatformFilter === 'All') {
      return this.currentSearchJobs;
    }
    return this.currentSearchJobs.filter(j => j.platform.toLowerCase() === this.selectedPlatformFilter.toLowerCase());
  }

  get filteredHistoryJobs(): JobSearchResult[] {
    if (this.historyPlatformFilter === 'All') {
      return this.historyJobs;
    }
    return this.historyJobs.filter(j => j.platform.toLowerCase() === this.historyPlatformFilter.toLowerCase());
  }

  get currentAppliedCount(): number {
    return this.currentSearchJobs.filter(j => j.isApplied).length;
  }

  get historyAppliedCount(): number {
    return this.historyJobs.filter(j => j.isApplied).length;
  }

  getScoreBadgeClass(score: number): string {
    if (score >= 90) return 'bg-emerald-500/10 text-emerald-400 border-emerald-500/30';
    if (score >= 75) return 'bg-sky-500/10 text-sky-400 border-sky-500/30';
    if (score >= 60) return 'bg-amber-500/10 text-amber-400 border-amber-500/30';
    return 'bg-rose-500/10 text-rose-400 border-rose-500/30';
  }

  getPlatformBadgeClass(platform: string): string {
    switch (platform.toLowerCase()) {
      case 'linkedin':
        return 'bg-[#0077b5]/15 text-[#38bdf8] border-[#0077b5]/30';
      case 'naukri':
        return 'bg-[#275df5]/15 text-[#818cf8] border-[#275df5]/30';
      case 'instahyre':
        return 'bg-[#10b981]/15 text-[#34d399] border-[#10b981]/30';
      default:
        return 'bg-purple-500/15 text-purple-300 border-purple-500/30';
    }
  }

  formatRelativeTime(dateStr: string): string {
    const date = new Date(dateStr);
    const diffHours = Math.round((new Date().getTime() - date.getTime()) / (1000 * 60 * 60));
    if (diffHours <= 1) return 'Just now';
    if (diffHours < 24) return `${diffHours}h ago`;
    const diffDays = Math.round(diffHours / 24);
    return `${diffDays}d ago`;
  }
}
