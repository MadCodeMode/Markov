import { createRouter, createWebHistory } from 'vue-router';
import Backtest from '../views/Backtest.vue';
import Home from '../views/Home.vue';

const routes = [
  {
    path: '/',
    name: 'Home',
    component: Home,
  },
  {
    path: '/backtest',
    name: 'Backtest',
    component: Backtest,
  },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

export default router;
