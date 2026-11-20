'use strict';

(() => {
    const source = document.getElementById('dashboard-chart-data');
    if (!source || typeof Chart === 'undefined') return;

    const data = JSON.parse(source.textContent);
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const fontFamily = getComputedStyle(document.body).fontFamily;
    const tooltip = {
        backgroundColor: '#172942',
        titleFont: { family: fontFamily, size: 12 },
        bodyFont: { family: fontFamily, size: 12 },
        padding: 12,
        cornerRadius: 8,
        displayColors: true,
        boxPadding: 5
    };

    const quotationCanvas = document.getElementById('quotation-activity-chart');
    if (quotationCanvas) {
        new Chart(quotationCanvas, {
            type: 'line',
            data: {
                labels: data.days.map(day => day.label),
                datasets: [
                    {
                        label: 'Prepared',
                        data: data.days.map(day => day.prepared),
                        borderColor: '#637d9d',
                        backgroundColor: 'rgba(99, 125, 157, 0.09)',
                        fill: true
                    },
                    {
                        label: 'Approved',
                        data: data.days.map(day => day.approved),
                        borderColor: '#aa6248',
                        backgroundColor: 'rgba(170, 98, 72, 0.04)',
                        fill: true
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                animation: reducedMotion ? false : { duration: 450 },
                interaction: { mode: 'index', intersect: false },
                elements: {
                    line: { tension: 0.32, borderWidth: 2 },
                    point: { radius: 3, hoverRadius: 5, backgroundColor: '#fff', borderWidth: 2 }
                },
                plugins: {
                    legend: {
                        position: 'bottom',
                        align: 'start',
                        labels: {
                            color: '#64748b',
                            usePointStyle: true,
                            pointStyle: 'circle',
                            boxWidth: 7,
                            boxHeight: 7,
                            padding: 22,
                            font: { family: fontFamily, size: 12 }
                        }
                    },
                    tooltip: {
                        ...tooltip,
                        callbacks: { title: items => data.days[items[0].dataIndex].date }
                    }
                },
                scales: {
                    x: {
                        border: { display: false },
                        grid: { display: false },
                        ticks: { color: '#64748b', font: { family: fontFamily, size: 11 } }
                    },
                    y: {
                        beginAtZero: true,
                        suggestedMax: 3,
                        border: { display: false, dash: [3, 4] },
                        grid: { color: '#edf0f5', drawTicks: false },
                        ticks: {
                            precision: 0,
                            maxTicksLimit: 5,
                            padding: 10,
                            color: '#94a3b8',
                            font: { family: fontFamily, size: 11 }
                        }
                    }
                }
            }
        });
    }

    const serviceCanvas = document.getElementById('requests-service-chart');
    if (serviceCanvas && data.categories.length) {
        const total = data.categories.reduce((sum, category) => sum + category.count, 0);
        new Chart(serviceCanvas, {
            type: 'doughnut',
            data: {
                labels: data.categories.map(category => category.name),
                datasets: [{
                    data: data.categories.map(category => category.count),
                    backgroundColor: data.categories.map(category => category.colour),
                    borderColor: '#fff',
                    borderWidth: 4,
                    borderRadius: 5,
                    hoverOffset: 5
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '77%',
                layout: { padding: 8 },
                animation: reducedMotion ? false : { duration: 450 },
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        ...tooltip,
                        callbacks: {
                            label: context => `${context.parsed} requests · ${Math.round(context.parsed / total * 100)}%`
                        }
                    }
                }
            },
            plugins: [{
                id: 'requestTotal',
                afterDraw(chart) {
                    const { ctx, chartArea } = chart;
                    const x = (chartArea.left + chartArea.right) / 2;
                    const y = (chartArea.top + chartArea.bottom) / 2;
                    ctx.save();
                    ctx.textAlign = 'center';
                    ctx.textBaseline = 'middle';
                    ctx.fillStyle = '#172942';
                    ctx.font = `700 32px ${fontFamily}`;
                    ctx.fillText(total, x, y - 8);
                    ctx.fillStyle = '#64748b';
                    ctx.font = `12px ${fontFamily}`;
                    ctx.fillText('Requests', x, y + 20);
                    ctx.restore();
                }
            }]
        });
    }
})();
