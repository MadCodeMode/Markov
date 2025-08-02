<template>
  <div id="app">
    <nav>
      <router-link to="/">Home</router-link> |
      <router-link to="/backtest">Backtest</router-link>
    </nav>
    <router-view/>
  </div>
</template>

<style scoped>
nav {
  padding: 1rem;
  text-align: center;
}
</style>
