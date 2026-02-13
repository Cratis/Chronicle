<!-- markdownlint-disable MD033 -->
# Test Coverage Statistics

This section contains test coverage statistics for the Chronicle project, tracking key metrics over time.

<style>
  .statistics-container {
    font-family: BlinkMacSystemFont,-apple-system,"Segoe UI",Roboto,Oxygen,Ubuntu,Cantarell,"Fira Sans","Droid Sans","Helvetica Neue",Helvetica,Arial,sans-serif;
    -webkit-font-smoothing: antialiased;
  }
  .statistics-header {
    margin-bottom: 16px;
    display: flex;
    flex-direction: column;
  }
  .header-item {
    margin: 4px 0;
  }
  .header-label {
    font-weight: bold;
    margin-right: 4px;
  }
  .coverage-set {
    margin: 8px 0;
    width: 100%;
    display: flex;
    flex-direction: column;
  }
  .coverage-graphs {
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;
    width: 100%;
    min-height: 360px;
  }
  .coverage-chart {
    max-width: 1200px;
    width: 100%;
    margin: 20px 0;
    height: 360px;
  }
  .chart-message {
    color: #666;
    font-size: 0.9rem;
    margin-top: 4px;
  }
  .project-toggles {
    display: flex;
    flex-wrap: wrap;
    justify-content: center;
    margin: 20px 0;
    gap: 8px;
  }
  .toggle-button {
    padding: 8px 16px;
    border-radius: 4px;
    border: 2px solid #3298dc;
    background-color: #3298dc;
    color: white;
    cursor: pointer;
  }
  .toggle-button.inactive {
    background-color: white;
    color: #3298dc;
  }
  .summary-stats {
    display: flex;
    justify-content: center;
    gap: 40px;
    margin: 20px 0;
    flex-wrap: wrap;
  }
  .stat-card {
    padding: 20px;
    border-radius: 8px;
    background-color: #f5f5f5;
    min-width: 150px;
    text-align: center;
  }
  .stat-value {
    font-size: 2rem;
    font-weight: bold;
    color: #3298dc;
  }
  .stat-label {
    font-size: 0.9rem;
    color: #666;
    margin-top: 8px;
  }
  .statistics-footer {
    margin-top: 16px;
    display: flex;
    align-items: center;
  }
  .download-button {
    color: #fff;
    background-color: #3298dc;
    border-color: transparent;
    cursor: pointer;
    text-align: center;
    padding: 8px 16px;
    margin: 4px;
    border-radius: 4px;
    border: none;
  }
  .download-button:hover {
    background-color: #2793da;
  }
  .spacer {
    flex: auto;
  }
  .small {
    font-size: 0.75rem;
  }
</style>

<div class="statistics-container">
  <div class="statistics-header" id="header">
    <div class="header-item">
      <strong class="header-label">Last Update:</strong>
      <span id="last-update"></span>
    </div>
    <div class="header-item">
      <strong class="header-label">Repository:</strong>
      <a id="repository-link" rel="noopener"></a>
    </div>
  </div>

  <div class="coverage-set">
    <div id="summary-stats" class="summary-stats"></div>

    <div class="project-toggles" id="project-toggles"></div>

    <div class="coverage-graphs">
      <canvas id="coverage-chart" class="coverage-chart"></canvas>
      <div id="chart-message" class="chart-message" role="status" aria-live="polite"></div>
    </div>
  </div>

  <div class="statistics-footer">
    <button id="dl-button" class="download-button">Download data as JSON</button>
    <div class="spacer"></div>
    <div class="small">Test coverage tracking for Chronicle</div>
  </div>
</div>

<script src="https://cdn.jsdelivr.net/npm/chart.js@3.9.1/dist/chart.min.js" defer></script>
<script src="coverage-data.js" defer></script>
<script src="coverage-page.js" defer></script>
