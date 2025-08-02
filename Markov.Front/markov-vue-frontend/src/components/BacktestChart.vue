<template>
  <div>
    <div class="saved-params-box" v-if="savedParameters.length > 0">
      <h3>Saved Parameters</h3>
      <div class="saved-params-grid">
        <button v-for="(params, index) in savedParameters" :key="index" @click="loadParameters(params)">
          {{ params.name }}
        </button>
      </div>
    </div>

    <div class="controls-grid">
      <!-- Control Groups -->
      <div class="control-group">
        <h3>General</h3>
        <label for="assetName">Asset Name:</label>
        <input id="assetName" v-model="assetName" type="text" />
        <label for="consecutiveMovements">Consecutive Movements:</label>
        <input id="consecutiveMovements" v-model.number="parameters.consecutiveMovements" type="number" />
        <label for="startingCapital">Starting Capital:</label>
        <input id="startingCapital" v-model.number="parameters.startingCapital" type="number" />
        <label for="tradeFeePercentage">Trade Fee (%):</label>
        <input id="tradeFeePercentage" v-model.number="parameters.tradeFeePercentage" type="number" step="0.001" />
        <label for="reinvestmentPercentage">Reinvestment (%):</label>
        <input id="reinvestmentPercentage" v-model.number="parameters.reinvestmentPercentage" type="number" step="0.1" />
      </div>

      <div class="control-group">
        <h3>Trade Sizing</h3>
        <label for="tradeSizeMode">Trade Size Mode:</label>
        <select id="tradeSizeMode" v-model.number="parameters.tradeSizeMode">
          <option value="0">Fixed Amount</option>
          <option value="1">Percentage Of Capital</option>
        </select>
        <label for="tradeSizeFixedAmount">Trade Size (Fixed):</label>
        <input id="tradeSizeFixedAmount" v-model.number="parameters.tradeSizeFixedAmount" type="number" :disabled="parameters.tradeSizeMode !== 0" />
        <label for="tradeSizePercentage">Trade Size (% of Capital):</label>
        <input id="tradeSizePercentage" v-model.number="parameters.tradeSizePercentage" type="number" step="0.01" :disabled="parameters.tradeSizeMode !== 1" />
      </div>

      <div class="control-group">
        <h3>Risk Targets</h3>
        <label for="stopLossPercentage">Stop Loss (%):</label>
        <input id="stopLossPercentage" v-model.number="parameters.stopLossPercentage" type="number" step="0.01" :disabled="parameters.enableAtrTargets" />
        <label for="takeProfitPercentage">Take Profit (%):</label>
        <input id="takeProfitPercentage" v-model.number="parameters.takeProfitPercentage" type="number" step="0.01" :disabled="parameters.enableAtrTargets" />
      </div>

      <div class="control-group">
        <h3>ATR Targets</h3>
        <label for="enableAtrTargets">Enable ATR Targets:</label>
        <input id="enableAtrTargets" v-model="parameters.enableAtrTargets" type="checkbox" />
        <label for="atrPeriod">ATR Period:</label>
        <input id="atrPeriod" v-model.number="parameters.atrPeriod" type="number" :disabled="!parameters.enableAtrTargets" />
        <label for="takeProfitAtrMultiplier">Take Profit (ATR Multiplier):</label>
        <input id="takeProfitAtrMultiplier" v-model.number="parameters.takeProfitAtrMultiplier" type="number" step="0.1" :disabled="!parameters.enableAtrTargets" />
        <label for="stopLossAtrMultiplier">Stop Loss (ATR Multiplier):</label>
        <input id="stopLossAtrMultiplier" v-model.number="parameters.stopLossAtrMultiplier" type="number" step="0.1" :disabled="!parameters.enableAtrTargets" />
      </div>

      <div class="control-group">
        <h3>Trend Filter</h3>
        <label for="enableTrendFilter">Enable Trend Filter:</label>
        <input id="enableTrendFilter" v-model="parameters.enableTrendFilter" type="checkbox" />
        <label for="longTermMAPeriod">Long-Term MA Period:</label>
        <input id="longTermMAPeriod" v-model.number="parameters.longTermMAPeriod" type="number" :disabled="!parameters.enableTrendFilter" />
      </div>

      <div class="control-group">
        <h3>RSI Filter</h3>
        <label for="enableRsiFilter">Enable RSI Filter:</label>
        <input id="enableRsiFilter" v-model="parameters.enableRsiFilter" type="checkbox" />
        <label for="rsiPeriod">RSI Period:</label>
        <input id="rsiPeriod" v-model.number="parameters.rsiPeriod" type="number" :disabled="!parameters.enableRsiFilter" />
        <label for="rsiOverboughtThreshold">RSI Overbought:</label>
        <input id="rsiOverboughtThreshold" v-model.number="parameters.rsiOverboughtThreshold" type="number" :disabled="!parameters.enableRsiFilter" />
        <label for="rsiOversoldThreshold">RSI Oversold:</label>
        <input id="rsiOversoldThreshold" v-model.number="parameters.rsiOversoldThreshold" type="number" :disabled="!parameters.enableRsiFilter" />
      </div>

      <div class="control-group">
        <h3>Volume Filter</h3>
        <label for="enableVolumeFilter">Enable Volume Filter:</label>
        <input id="enableVolumeFilter" v-model="parameters.enableVolumeFilter" type="checkbox" />
        <label for="volumeMAPeriod">Volume MA Period:</label>
        <input id="volumeMAPeriod" v-model.number="parameters.volumeMAPeriod" type="number" :disabled="!parameters.enableVolumeFilter" />
        <label for="minVolumeMultiplier">Min Volume Multiplier:</label>
        <input id="minVolumeMultiplier" v-model.number="parameters.minVolumeMultiplier" type="number" step="0.1" :disabled="!parameters.enableVolumeFilter" />
      </div>
    </div>

    <button @click="fetchData" :disabled="loading" class="run-button">
      {{ loading ? 'Loading...' : 'Run Backtest' }}
    </button>
    <button @click="saveParameters" class="run-button">Save Parameters</button>
    <button @click="resetZoom">Reset Zoom</button>
    <button @click="toggleFullscreen">
      {{ isFullscreen ? 'Exit Fullscreen' : 'Fullscreen' }}
    </button>

    <div v-if="error" class="error-message">
      <p>Error fetching data: {{ error.message }}</p>
      <p>Please check the console for more details.</p>
    </div>

    <div ref="fullscreenWrapper" class="chart-wrapper">
      <div v-if="backtestResult" class="results-box">
        <h2>Backtest Results</h2>
        <div class="results-grid">
          <p><strong>Starting Capital:</strong> {{ backtestResult.startingCapital.toFixed(2) }}</p>
          <p><strong>Final Trading Capital:</strong> {{ backtestResult.finalTradingCapital.toFixed(2) }}</p>
          <p><strong>Realized PNL:</strong> {{ backtestResult.realizedPNL.toFixed(2) }}</p>
          <p><strong>Wins:</strong> {{ backtestResult.winCount }}</p>
          <p><strong>Losses:</strong> {{ backtestResult.lossCount }}</p>
          <p><strong>Hold Moves:</strong> {{ backtestResult.holdMoveCount }}</p>
          <p><strong>Held Asset Quantity:</strong> {{ backtestResult.finalHoldAccountAssetQuantity.toFixed(8) }}</p>
          <p><strong>Held Asset Value:</strong> {{ backtestResult.finalHoldAccountValue.toFixed(2) }}</p>
        </div>
      </div>
      <div class="chart-container">
        <canvas ref="chartRef"></canvas>
      </div>
    </div>

    <div v-if="backtestResult && backtestResult.tradeHistory.length > 0" class="trade-history">
      <h2>Trade History</h2>
      <table>
        <thead>
          <tr>
            <th>Timestamp</th>
            <th>Signal</th>
            <th>Outcome</th>
            <th>Entry Price</th>
            <th>Exit Price</th>
            <th>Amount Invested</th>
            <th>PNL</th>
            <th>Notes</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(trade, index) in backtestResult.tradeHistory" :key="index">
            <td>{{ new Date(trade.timestamp).toLocaleString() }}</td>
            <td>{{ trade.signal }}</td>
            <td>{{ trade.outcome }}</td>
            <td>{{ trade.entryPrice.toFixed(2) }}</td>
            <td>{{ trade.exitPrice.toFixed(2) }}</td>
            <td>{{ trade.amountInvested.toFixed(2) }}</td>
            <td :class="{ 'win': trade.pnl > 0, 'loss': trade.pnl < 0 }">{{ trade.pnl.toFixed(2) }}</td>
            <td>{{ trade.notes }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, reactive } from 'vue';
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
} from 'chart.js';
import { CandlestickController, CandlestickElement } from 'chartjs-chart-financial';
import zoomPlugin from 'chartjs-plugin-zoom';
import annotationPlugin from 'chartjs-plugin-annotation';
import 'chartjs-adapter-luxon';

Chart.register(
  TimeScale,
  LinearScale,
  Tooltip,
  Legend,
  BarController,
  BarElement,
  PointElement,
  LineController,
  CandlestickController,
  CandlestickElement,
  zoomPlugin,
  annotationPlugin
);

const assetName = ref('BTCUSDT');
const chartRef = ref();
let chartInstance = null;

const loading = ref(false);
const error = ref(null);
const backtestResult = ref(null);
const isFullscreen = ref(false);
const fullscreenWrapper = ref(null);
const savedParameters = ref([]);

const parameters = reactive({
  consecutiveMovements: 3,
  startingCapital: 10000,
  tradeFeePercentage: 0.001,
  reinvestmentPercentage: 1.0,
  tradeSizeMode: 1, // PercentageOfCapital
  tradeSizeFixedAmount: 1000,
  tradeSizePercentage: 0.1,
  stopLossPercentage: 0.02,
  takeProfitPercentage: 0.04,
  enableAtrTargets: false,
  atrPeriod: 14,
  takeProfitAtrMultiplier: 2.0,
  stopLossAtrMultiplier: 1.5,
  enableTrendFilter: false,
  longTermMAPeriod: 50,
  enableRsiFilter: false,
  rsiPeriod: 14,
  rsiOverboughtThreshold: 70,
  rsiOversoldThreshold: 30,
  enableVolumeFilter: false,
  volumeMAPeriod: 20,
  minVolumeMultiplier: 1.5,
});

const saveParameters = () => {
  const name = prompt("Enter a name for these parameters:");
  if (name) {
    const existingIndex = savedParameters.value.findIndex(p => p.name === name);
    // Create a plain object from the reactive one for saving
    const newSave = { name, ...JSON.parse(JSON.stringify(parameters)) };

    if (existingIndex !== -1) {
      // Update existing item
      savedParameters.value[existingIndex] = newSave;
    } else {
      // Add as a new item
      savedParameters.value.push(newSave);
    }
    localStorage.setItem('markovBacktestSaves', JSON.stringify(savedParameters.value));
  }
};

const loadParameters = (paramsToLoad) => {
  // Destructure to separate the name from the actual parameters
  const { name, ...paramsOnly } = paramsToLoad;
  Object.assign(parameters, paramsOnly);
  fetchData();
};

const loadSavedParameters = () => {
  const saves = localStorage.getItem('markovBacktestSaves');
  if (saves) {
    savedParameters.value = JSON.parse(saves);
  }
};

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

const resetZoom = () => {
  if (chartInstance) {
    chartInstance.resetZoom();
  }
};

const fetchData = async () => {
  loading.value = true;
  error.value = null;
  try {
    const assetResPromise = axios.get(`http://localhost:8080/Markov/assets`);
    const backtestResPromise = axios.post(
      `http://localhost:8080/Markov/calc-reversal/${assetName.value}/backtest`,
      parameters
    );

    const [assetRes, backtestRes] = await Promise.all([assetResPromise, backtestResPromise]);

    const historicalData = assetRes.data.historicalData;
    backtestResult.value = backtestRes.data;

    const priceData = historicalData.map(d => ({
      x: new Date(d.timestamp).valueOf(),
      o: d.open,
      h: d.high,
      l: d.low,
      c: d.close
    }));

    const volumeData = historicalData.map(d => ({
      x: new Date(d.timestamp).valueOf(),
      y: d.volume
    }));

    if (chartInstance) {
      chartInstance.destroy();
    }

    const generateTradeAnnotations = (trades) => {
      return trades.map((trade, i) => {
        const isWin = trade.pnl > 0;
        const color = isWin ? 'green' : 'red';
        const arrow = isWin ? '↑' : '↓';

        return {
          type: 'label',
          xValue: new Date(trade.timestamp).valueOf(),
          yValue: trade.entryPrice,
          content: [arrow],
          color: color,
          font: { weight: 'bold', size: 16 },
          textAlign: 'center',
          yAdjust: -20,
          id: `trade-${i}`,
          custom: {
            tradeDetails: trade
          }
        };
      });
    };

    const annotations = generateTradeAnnotations(backtestResult.value.tradeHistory);

    chartInstance = new Chart(chartRef.value, {
      type: 'candlestick',
      data: {
        datasets: [
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
        ]
      },
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
                const annotation = context.chart.options.plugins.annotation.annotations[context.dataIndex];
                if (annotation && annotation.custom && annotation.custom.tradeDetails) {
                  const details = annotation.custom.tradeDetails;
                  return [
                    `Signal: ${details.signal}`,
                    `Entry: ${details.entryPrice.toFixed(2)}`,
                    `Exit: ${details.exitPrice.toFixed(2)}`,
                    `PNL: ${details.pnl.toFixed(2)}`,
                    `Invested: ${details.amountInvested.toFixed(2)}`,
                    `Notes: ${details.notes}`
                  ];
                }
                return `${context.dataset.label}: ${context.formattedValue}`;
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
          },
          annotation: {
            annotations: annotations
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

onMounted(() => {
  loadSavedParameters();
  fetchData();
});

</script>

<style scoped>
.saved-params-box {
  border: 1px solid #ccc;
  padding: 1rem;
  margin-bottom: 1rem;
  border-radius: 5px;
  background-color: transparent;
}
.saved-params-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}
.controls-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 1.5rem;
  margin-bottom: 1rem;
}
.control-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  border: 1px solid #ccc;
  padding: 1rem;
  border-radius: 5px;
}
.control-group h3 {
  margin-top: 0;
  margin-bottom: 1rem;
  border-bottom: 1px solid #eee;
  padding-bottom: 0.5rem;
}
.run-button, button {
  margin-top: 1rem;
  padding: 0.75rem 1.5rem;
  font-size: 1rem;
  cursor: pointer;
}
.error-message {
  color: red;
  border: 1px solid red;
  padding: 1rem;
  margin-top: 1rem;
}
.results-box {
  border: 1px solid #ccc;
  padding: 1rem;
  margin-top: 1rem;
  border-radius: 5px;
  background-color: transparent;
}
.results-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 0.5rem;
}
.results-box h2 {
  margin-top: 0;
  border-bottom: 1px solid #eee;
  padding-bottom: 0.5rem;
  margin-bottom: 1rem;
}
.chart-wrapper {
  position: relative;
  width: 100%;
  height: 80vh; /* Default height */
}
.chart-container {
  position: relative;
  height: 100%;
  width: 100%;
}
canvas {
  display: block;
  width: 100% !important;
  height: 100% !important;
}
.chart-wrapper:fullscreen {
  background: transparent;
  padding: 1rem;
  z-index: 9999;
}
.chart-wrapper:fullscreen .chart-container {
  height: 90vh;
}
.trade-history {
    margin-top: 17rem;
  }
.trade-history table {
  width: 100%;
  border-collapse: collapse;
}
.trade-history th, .trade-history td {
  border: 1px solid #ccc;
  padding: 0.5rem;
  text-align: left;
}
.trade-history th {
  background-color: #0a0404;
}
.win {
  color: green;
}
.loss {
  color: red;
}
</style>

