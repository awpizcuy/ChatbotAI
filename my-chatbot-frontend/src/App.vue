<template>
  <header>
    <h1>Website Chatbot AI Saya</h1>
  </header>
  <main>
    <Chatbot />
  </main>
</template>

<script setup lang="ts">
import Chatbot from './components/AIChatbot.vue';
</script>

<style>
/* Styling global atau umum untuk App.vue */
body {
  font-family: Arial, sans-serif;
  margin: 0;
  background-color: #f4f4f4;
  display: flex;
  flex-direction: column;
  align-items: center;
  min-height: 100vh;
}
header {
  width: 100%;
  text-align: center;
  padding: 20px;
  background-color: #333;
  color: white;
  margin-bottom: 20px;
}
main {
  flex-grow: 1;
  width: 100%;
  display: flex;
  justify-content: center;
}
</style>
