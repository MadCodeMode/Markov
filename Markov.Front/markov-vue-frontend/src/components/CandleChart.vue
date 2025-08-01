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

    <div v-if="!error">
        <h2>Reversal Probabilities</h2>
        <p>Up Reversal: {{ upReversalPercentage }}%</p>
        <p>Down Reversal: {{ downReversalPercentage }}%</p>
        <div class="chart-container">
          <canvas ref="chartRef"></canvas>
        </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, nextTick, onBeforeUnmount } from 'vue';
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
import 'chartjs-adapter-luxon';
import { CandlestickController, CandlestickElement } from 'chartjs-chart-financial';

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
  BubbleController
);

const assetName = ref('BTCUSDT');
const consecutiveMovements = ref(3);
const chartRef = ref(null);
let chartInstance = null;

const loading = ref(false);
const error = ref(null);
const upReversalPercentage = ref(0);
const downReversalPercentage = ref(0);

const destroyChart = () => {
  if (chartInstance) {
    chartInstance.destroy();
    chartInstance = null;
  }
};

const fetchData = async () => {
  loading.value = true;
  error.value = null;
  try {
    const now = new Date().toISOString();
    const earlier = new Date(Date.now() - 1000 * 86400000).toISOString();

    const [assetRes, reversalRes] = await Promise.all([
      axios.get(`http://localhost:8080/Markov/assets?startDate=${earlier}`),
      axios.get(
        `http://localhost:8080/Markov/calc-reversal/${assetName.value}?consecutiveMovements=${consecutiveMovements.value}`
      ),
    ]);

    const historicalData = assetRes.data.historicalData;
    const reversalData = reversalRes.data;

    if (!historicalData || historicalData.length === 0) {
      throw new Error('Historical data is empty or not available.');
    }

    upReversalPercentage.value = (reversalData.upReversalPercentage * 100).toFixed(2);
    downReversalPercentage.value = (reversalData.downReversalPercentage * 100).toFixed(2);

    const priceData = historicalData.map((d) => ({
      x: new Date(d.timestamp).valueOf(),
      o: d.open,
      h: d.high,
      l: d.low,
      c: d.close,
    }));

    const volumeData = historicalData.map((d) => ({
      x: new Date(d.timestamp).valueOf(),
      y: d.volume,
    }));

    const getClosePriceForTimestamp = (timestamp) => {
      const price = priceData.find((p) => p.x === new Date(timestamp).valueOf());
      return price ? price.c : null;
    };

    const createReversalDataset = (label, data, color) => ({
      type: 'bubble',
      label,
      data: data
        .map((d) => ({
          x: new Date(d.timestamp).valueOf(),
          y: getClosePriceForTimestamp(d.timestamp),
          r: d.tradeCount / 1000,
        }))
        .filter((d) => d.y !== null),
      backgroundColor: color,
    });

    await nextTick();

    destroyChart(); // Clean up previous chart

    chartInstance = new Chart(chartRef.value.getContext('2d'), {
      type: 'candlestick',
      data: {
        datasets: [
          {
            label: 'Price',
            data: priceData,
            type: 'candlestick',
          },
          {
            label: 'Volume',
            data: volumeData,
            type: 'bar',
            yAxisID: 'volume',
            backgroundColor: 'rgba(0, 123, 255, 0.3)',
          },
          createReversalDataset('Up Reversal', reversalData.upReversalData, 'rgba(75, 192, 192, 0.7)'),
          createReversalDataset('Down Reversal', reversalData.downReversalData, 'rgba(255, 99, 132, 0.7)'),
          createReversalDataset('Up Non-Reversal', reversalData.upNonReversalData, 'rgba(75, 192, 192, 0.2)'),
          createReversalDataset('Down Non-Reversal', reversalData.downNonReversalData, 'rgba(255, 99, 132, 0.2)'),
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          x: { type: 'time', time: { unit: 'day' } },
          y: { position: 'left', title: { display: true, text: 'Price' } },
          volume: {
            position: 'right',
            title: { display: true, text: 'Volume' },
            grid: { drawOnChartArea: false },
            min: 0,
          },
        },
        plugins: {
          tooltip: {
            callbacks: {
              label(context) {
                const datasetLabel = context.dataset.label || '';
                if (context.dataset.type === 'bubble') {
                  const d = context.raw;
                  return `${datasetLabel}: (Price: ${d.y}, Trades: ${d.r * 1000})`;
                }
                if (context.dataset.type === 'candlestick') {
                  const d = context.raw;
                  return `${datasetLabel}: O: ${d.o} H: ${d.h} L: ${d.l} C: ${d.c}`;
                }
                return `${datasetLabel}: ${context.formattedValue}`;
              },
            },
          },
          legend: {
            display: true,
          },
        },
      },
    });
  } catch (e) {
    console.error('Error fetching data:', e);
    error.value = e;
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  fetchData();
});

onBeforeUnmount(() => {
  destroyChart();
});
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
</style>
