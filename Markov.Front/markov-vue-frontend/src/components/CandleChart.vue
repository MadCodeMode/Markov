<template>
  <div>
    <div class="controls">
      <label for="assetName">Asset Name:</label>
      <input id="assetName" v-model="assetName" type="text" />
      <label for="consecutiveMovements">Consecutive Movements:</label>
      <input id="consecutiveMovements" v-model.number="consecutiveMovements" type="number" />
      <button @click="fetchData" :disabled="loading">
        {{ loading ? 'Loading...' : 'Update Chart' }}
      </button>
    </div>

    <div v-if="error" class="error-message">
      <p>Error fetching data: {{ error.message }}</p>
      <p>Please check the console for more details.</p>
    </div>

    <button @click="resetZoom">Reset Zoom</button>

<button @click="toggleFullscreen">
  {{ isFullscreen ? 'Exit Fullscreen' : 'Fullscreen' }}
</button>

<div ref="fullscreenWrapper" class="chart-wrapper">

    <div v-if="!error">
        <h2>Reversal Probabilities</h2>
        <p>Up Reversal: {{ upReversalPercentage }}%</p>
        <p>Down Reversal: {{ downReversalPercentage }}%</p>
        <div class="chart-container">
          <canvas ref="chartRef"></canvas>
        </div>
    </div>
  </div>
</div>

</template>

<script setup>
import annotationPlugin from 'chartjs-plugin-annotation';
import { ref, onMounted } from 'vue';
import axios from 'axios';
import {
  Chart,
  TimeScale,
  LinearScale,
  Tooltip,
  Legend,
  BarController,
  BarElement,
  PointElement,
  LineController,
  BubbleController,
} from 'chart.js';
import { CandlestickController, CandlestickElement } from 'chartjs-chart-financial';
import zoomPlugin from 'chartjs-plugin-zoom';
import 'chartjs-adapter-luxon';

Chart.register(
  TimeScale,
  LinearScale,
  Tooltip,
  Legend,
  BarController,
  BarElement,
  CandlestickController,
  CandlestickElement,
  PointElement,
  LineController,
  BubbleController,
  zoomPlugin,
  annotationPlugin
);

const assetName = ref('BTCUSDT');
const consecutiveMovements = ref(3);
const chartRef = ref();
let chartInstance = null;

const loading = ref(false);
const error = ref(null);
const upReversalPercentage = ref(0);
const downReversalPercentage = ref(0);

const resetZoom = () => {
  if (chartInstance) {
    chartInstance.resetZoom();
  }
};

const isFullscreen = ref(false);
const fullscreenWrapper = ref(null);

const toggleFullscreen = async () => {
  const el = fullscreenWrapper.value;

  if (!document.fullscreenElement) {
    await el.requestFullscreen();
    isFullscreen.value = true;
  } else {
    await document.exitFullscreen();
    isFullscreen.value = false;
  }
};

document.addEventListener('fullscreenchange', () => {
  isFullscreen.value = !!document.fullscreenElement;
});

const fetchData = async () => {
  loading.value = true;
  error.value = null;
  try {

    const assetResPromise = axios.get(`http://localhost:8080/Markov/assets`);
    const reversalResPromise = axios.post(`http://localhost:8080/Markov/calc-reversal/${assetName.value}`, {
      consecutiveMovements: consecutiveMovements.value
    });

    const [assetRes, reversalRes] = await Promise.all([assetResPromise, reversalResPromise]);

    const historicalData = assetRes.data.historicalData;
    const reversalData = reversalRes.data;

    upReversalPercentage.value = (reversalData.upReversalPercentage * 100).toFixed(2);
    downReversalPercentage.value = (reversalData.downReversalPercentage * 100).toFixed(2);

    const priceData = historicalData.map(d => ({
      x: new Date(d.timestamp).valueOf(),
      o: d.open,
      h: d.high,
      l: d.low,
      c: d.close
    }));

    const maxPrice = Math.max(...priceData.map(p => p.h));
    const bubbleOffset = maxPrice * 0.05; // 5% above highest price

    const generateArrowAnnotations = (data, type) => {
    const color = {
      'upReversal': 'green',
      'upNonReversal': 'red',
      'downReversal': 'green',
      'downNonReversal': 'red',
    }[type];

    const direction = {
      'upReversal': '↓',
      'upNonReversal': '↑',
      'downReversal': '↑',
      'downNonReversal': '↓',
    }[type];

    const closeFor = (ts) => getClosePriceForTimestamp(ts);

    return data.map((d, i) => {
      const y = closeFor(d.timestamp);
      if (y === null) return null;

      return {
        type: 'label',
        xValue: new Date(d.timestamp).valueOf(),
        yValue: y + bubbleOffset * 1.5,
        content: [direction],
        color,
        font: { weight: 'bold', size: 16 },
        textAlign: 'center',
        yAdjust: -10,
        xAdjust: 0,
        drawTime: 'afterDatasetsDraw',
        id: `${type}-${i}`,
      };
    }).filter(Boolean);
  };

    const volumeData = historicalData.map(d => ({
      x: new Date(d.timestamp).valueOf(),
      y: d.volume
    }));

    const getClosePriceForTimestamp = (timestamp) => {
      const price = priceData.find(p => p.x === new Date(timestamp).valueOf());
      return price ? price.c : null;
    };

    const createReversalDataset = (label, data, color) => ({
      type: 'bubble',
      label,
      parsing: false,
      data: data.map(d => {
        const close = getClosePriceForTimestamp(d.timestamp);
        return close !== null ? {
          x: new Date(d.timestamp).valueOf(),
          y: close + bubbleOffset,
          r: d.tradeCount / 1000
        } : null;
      }).filter(d => d !== null),
      backgroundColor: color
    });

    const datasets = [
  {
    label: 'Price',
    data: priceData,
    type: 'candlestick',
    parsing: false,
  },
  {
    label: 'Volume',
    data: volumeData,
    type: 'bar',
    yAxisID: 'volume',
    backgroundColor: 'rgba(0, 123, 255, 0.3)',
    parsing: true,
  },
  createReversalDataset('Up Reversal', reversalData.upReversalData, 'rgba(0, 153, 255, 0.8)'),
  createReversalDataset('Down Reversal', reversalData.downReversalData, 'rgba(255, 140, 0, 0.8)'),
  createReversalDataset('Up Non-Reversal', reversalData.upNonReversalData, 'rgba(0, 153, 255, 0.3)'),
  createReversalDataset('Down Non-Reversal', reversalData.downNonReversalData, 'rgba(255, 140, 0, 0.3)'),
];

    if (chartInstance) {
      chartInstance.destroy();
    }

    const annotations = [
      ...generateArrowAnnotations(reversalData.upReversalData, 'upReversal'),
      ...generateArrowAnnotations(reversalData.downReversalData, 'downReversal'),
      ...generateArrowAnnotations(reversalData.upNonReversalData, 'upNonReversal'),
      ...generateArrowAnnotations(reversalData.downNonReversalData, 'downNonReversal'),
    ];

    chartInstance = new Chart(chartRef.value, {
      type: 'candlestick',
      data: { datasets },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        parsing: false,
        scales: {
          x: {
            type: 'time',
            time: { unit: 'day' },
            title: { display: true, text: 'Time' },
          },
          y: {
            position: 'left',
            title: { display: true, text: 'Price' }
          },
          volume: {
            position: 'right',
            title: { display: true, text: 'Volume' },
            grid: { drawOnChartArea: false },
            min: 0,
          }
        },
        plugins: {
          tooltip: {
             callbacks: {
              label: function (context) {
                const datasetLabel = context.dataset.label || '';
                if (context.dataset.type === 'bubble') {
                  const d = context.raw;
                  return `${datasetLabel} → Trades: ${Math.round(d.r * 1000)}`;
                }
                return `${datasetLabel}: ${context.formattedValue}`;
              }
            }
          },
          legend: { display: true },
          zoom: {
            pan: {
              enabled: true,
              mode: 'x',
              modifierKey: 'ctrl',
            },
            zoom: {
              wheel: { enabled: true },
              pinch: { enabled: true },
              mode: 'x',
            },
            limits: {
              x: { minRange: 24 * 60 * 60 * 1000 }, // min 1 day
            }
          },
           annotation: {
            annotations
          }
        },
        elements: {
    candlestick: {
      color: {
        up: '#00cc44',     // bright green
        down: '#ff4444'    // red
      },
      borderColor: {
        up: '#00cc44',
        down: '#ff4444'
      },
    }
  }
      }
    });

  } catch (e) {
    console.error('Error fetching data:', e);
    error.value = e;
  } finally {
    loading.value = false;
  }
};

onMounted(fetchData);
</script>


<style scoped>
.controls {
  margin-bottom: 1rem;
  display: flex;
  gap: 1rem;
  align-items: center;
  flex-wrap: wrap;
}
.error-message {
  color: red;
  border: 1px solid red;
  padding: 1rem;
  margin-top: 1rem;
}
.chart-container {
  position: relative;
  height: 800px;
  width: 100%;
}
canvas {
  display: block;
  width: 100% !important;
  height: 100% !important;
}

.chart-wrapper:fullscreen {
  background: #111;
  padding: 1rem;
  z-index: 9999;
}

.chart-wrapper:fullscreen .chart-container {
  height: 90vh;
}
</style>
