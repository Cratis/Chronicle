'use strict';
(function() {
  const projectColors = [
    '#178600', // .NET green
    '#3298dc', // Blue
    '#f1e05a', // Yellow
    '#f34b7d', // Pink
    '#a270ba', // Purple
    '#dea584', // Tan
    '#00add8', // Cyan
    '#ff6b6b', // Red
    '#4ecdc4', // Teal
    '#95e1d3', // Mint
  ];

  let chart = null;
  let chartData = null;
  let activeProjects = new Set();

  function setMessage(message) {
    const messageElement = document.getElementById('chart-message');
    if (messageElement) {
      messageElement.textContent = message || '';
    }
  }

  function setSummaryMessage(message) {
    const summaryContainer = document.getElementById('summary-stats');
    if (summaryContainer) {
      summaryContainer.innerHTML = message ? `<p>${message}</p>` : '';
    }
  }

  function disableDownload() {
    const button = document.getElementById('dl-button');
    if (button) {
      button.disabled = true;
    }
  }

  function init() {
    const data = window.COVERAGE_DATA;
    if (!data) {
      setSummaryMessage('Coverage data is not available.');
      setMessage('Coverage data could not be loaded.');
      disableDownload();
      return null;
    }

    const lastUpdate = data.lastUpdate ? new Date(data.lastUpdate).toString() : 'No data yet';
    document.getElementById('last-update').textContent = lastUpdate;
    const repoLink = document.getElementById('repository-link');
    repoLink.href = data.repoUrl;
    repoLink.textContent = data.repoUrl;
    repoLink.target = '_blank';

    document.getElementById('dl-button').onclick = () => {
      const dataUrl = 'data:application/json,' + JSON.stringify(data, null, 2);
      const a = document.createElement('a');
      a.href = dataUrl;
      a.download = 'coverage_data.json';
      a.click();
    };

    return data;
  }

  function prepareCoverageData(data) {
    if (!data.entries || Object.keys(data.entries).length === 0) {
      return { projects: [], dates: [], coverageByProject: {} };
    }

    const allDates = new Set();
    const projects = Object.keys(data.entries);
    const coverageByProject = {};

    projects.forEach(project => {
      const entries = data.entries[project] || [];
      coverageByProject[project] = {};

      entries.forEach(entry => {
        const date = new Date(entry.date).toLocaleDateString();
        allDates.add(date);
        coverageByProject[project][date] = entry.lineCoverage;
      });
    });

    const sortedDates = Array.from(allDates).sort((a, b) => new Date(a) - new Date(b));

    return {
      projects,
      dates: sortedDates,
      coverageByProject
    };
  }

  function renderSummaryStats(currentChartData) {
    const summaryContainer = document.getElementById('summary-stats');
    summaryContainer.innerHTML = '';

    if (currentChartData.projects.length === 0) {
      summaryContainer.innerHTML = '<p>No coverage data available yet.</p>';
      return;
    }

    let totalCoverage = 0;
    let projectCount = 0;

    currentChartData.projects.forEach(project => {
      const dates = currentChartData.dates;
      if (dates.length > 0) {
        const latestDate = dates[dates.length - 1];
        const coverage = currentChartData.coverageByProject[project][latestDate];
        if (coverage !== undefined) {
          totalCoverage += coverage;
          projectCount++;
        }
      }
    });

    const avgCoverage = projectCount > 0 ? (totalCoverage / projectCount).toFixed(1) : 0;

    const stats = [
      { label: 'Average Coverage', value: avgCoverage + '%' },
      { label: 'Projects Tracked', value: currentChartData.projects.length },
      { label: 'Data Points', value: currentChartData.dates.length }
    ];

    stats.forEach(stat => {
      const card = document.createElement('div');
      card.className = 'stat-card';
      card.innerHTML = `
        <div class="stat-value">${stat.value}</div>
        <div class="stat-label">${stat.label}</div>
      `;
      summaryContainer.appendChild(card);
    });
  }

  function renderProjectToggles(currentChartData) {
    const toggleContainer = document.getElementById('project-toggles');
    toggleContainer.innerHTML = '';

    if (currentChartData.projects.length === 0) {
      return;
    }

    activeProjects = new Set(currentChartData.projects);

    currentChartData.projects.forEach((project, index) => {
      const button = document.createElement('button');
      button.className = 'toggle-button';
      button.textContent = project;
      button.style.borderColor = projectColors[index % projectColors.length];

      button.onclick = () => {
        if (activeProjects.has(project)) {
          activeProjects.delete(project);
          button.classList.add('inactive');
        } else {
          activeProjects.add(project);
          button.classList.remove('inactive');
        }
        updateChart();
      };

      toggleContainer.appendChild(button);
    });
  }

  function createChart(currentChartData) {
    const canvas = document.getElementById('coverage-chart');
    const ctx = canvas.getContext('2d');

    if (currentChartData.projects.length === 0) {
      setMessage('No coverage data available yet.');
      return;
    }

    if (typeof Chart === 'undefined') {
      setSummaryMessage('Chart rendering is unavailable.');
      setMessage('Chart.js could not be loaded.');
      return;
    }

    const datasets = currentChartData.projects.map((project, index) => ({
      label: project,
      data: currentChartData.dates.map(date => currentChartData.coverageByProject[project][date] || null),
      borderColor: projectColors[index % projectColors.length],
      backgroundColor: projectColors[index % projectColors.length] + '40',
      tension: 0.1,
      hidden: false
    }));

    chart = new Chart(ctx, {
      type: 'line',
      data: {
        labels: currentChartData.dates,
        datasets: datasets
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          title: {
            display: true,
            text: 'Test Coverage Over Time'
          },
          legend: {
            display: true,
            position: 'bottom'
          },
          tooltip: {
            callbacks: {
              label: function(context) {
                let label = context.dataset.label || '';
                if (label) {
                  label += ': ';
                }
                if (context.parsed.y !== null) {
                  label += context.parsed.y.toFixed(1) + '%';
                }
                return label;
              }
            }
          }
        },
        scales: {
          y: {
            beginAtZero: true,
            max: 100,
            title: {
              display: true,
              text: 'Coverage (%)'
            }
          },
          x: {
            title: {
              display: true,
              text: 'Date'
            }
          }
        }
      }
    });
  }

  function updateChart() {
    if (!chart) {
      return;
    }

    chart.data.datasets.forEach((dataset, index) => {
      const project = chartData.projects[index];
      dataset.hidden = !activeProjects.has(project);
    });

    chart.update();
  }

  function start() {
    const data = init();
    if (!data) {
      return;
    }

    chartData = prepareCoverageData(data);
    renderSummaryStats(chartData);
    renderProjectToggles(chartData);
    createChart(chartData);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', start);
  } else {
    start();
  }
})();
