window.iamCharts = window.iamCharts || {
  renderRadar: function (id, labels, values) {
    const el = document.getElementById(id);
    if (!el || !window.echarts) return;
    const chart = window.echarts.init(el);
    chart.setOption({
      tooltip: {},
      radar: { indicator: labels.map(l => ({ name: l, max: 100 })) },
      series: [{ type: 'radar', data: [{ value: values }] }]
    });
  },
  renderHeatmap: function (id, rows) {
    const el = document.getElementById(id);
    if (!el || !window.echarts) return;
    const chart = window.echarts.init(el);
    const labels = rows.map(r => r.label);
    const bands = ['Red', 'Amber', 'Green'];
    const points = rows.map((r, i) => [bands.indexOf(r.band), i, r.value]);

    chart.setOption({
      tooltip: {},
      xAxis: { type: 'category', data: bands },
      yAxis: { type: 'category', data: labels },
      visualMap: { min: 0, max: 100, calculable: true, orient: 'horizontal', left: 'center', bottom: 0 },
      series: [{ type: 'heatmap', data: points, label: { show: true, formatter: function (p) { return p.value[2].toFixed(0) + '%'; } } }]
    });
  }
};
