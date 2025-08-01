<template>
  <canvas ref="chartRef"></canvas>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import axios from 'axios'
import { Chart, TimeScale, LinearScale, Tooltip, Legend, BarController, BarElement } from 'chart.js'
import 'chartjs-adapter-luxon'
import {
  CandlestickController,
  CandlestickElement
} from 'chartjs-chart-financial'

Chart.register(
  TimeScale,
  LinearScale,
  Tooltip,
  Legend,
  BarController,
  BarElement,
  CandlestickController,
  CandlestickElement
)

const props = defineProps({ assetName: String })
const chartRef = ref()

onMounted(async () => {
  const now = new Date().toISOString()
  const earlier = new Date(Date.now() - 1000 * 86400000).toISOString()

  const res = await axios.get(
    `http://localhost:8080/Markov/assets?startDate=${earlier}`
  )

  const priceData = res.data.historicalData.map(d => ({
    x: d.timestamp, // ISO 8601 timestamp
    o: d.open,
    h: d.high,
    l: d.low,
    c: d.close
  }))

  const volumeData = res.data.historicalData.map(d => ({
    x: d.timestamp,
    y: d.volume
  }))

  new Chart(chartRef.value, {
    type: 'candlestick',
    data: {
      datasets: [
        {
          label: 'Price',
          data: priceData
        },
        {
          type: 'bar',
          label: 'Volume',
          data: volumeData,
          yAxisID: 'volume',
          backgroundColor: 'rgba(0, 123, 255, 0.3)',
        }
      ]
    },
    options: {
      scales: {
        x: { type: 'time' },
        y: { position: 'left', title: { display: true, text: 'Price' } },
        volume: {
          position: 'right',
          title: { display: true, text: 'Volume' },
          grid: { drawOnChartArea: false }
        }
      },
      plugins: {
        legend: { display: false }
      }
    }
  })
})
</script>
