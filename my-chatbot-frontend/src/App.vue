<template>
  <header>
    <h1>Website Chatbot AI</h1>
  </header>
  <main>
    <AIChatbot />
  </main>
</template>

<script setup lang="ts">
import AIChatbot from './components/AIChatbot.vue';
</script>

<style>
/* Styling global atau umum untuk App.vue */
@import url('https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600&display=swap');

body {
  font-family: 'Poppins', sans-serif; /* Menggunakan font yang lebih modern */
  margin: 0;
  padding: 0; /* Pastikan padding body 0 untuk kontrol penuh */
  background: linear-gradient(135deg, #f0f2f5 0%, #e0e6ed 100%); /* Latar belakang gradient */
  display: flex;
  flex-direction: column;
  align-items: center;
  min-height: 100vh;
  color: #333;
}
header {
  width: 100%;
  text-align: center;
  padding: 25px 0; /* Padding lebih banyak */
  background-color: #2c3e50; /* Warna header lebih gelap */
  color: white;
  margin-bottom: 30px; /* Margin lebih besar */
  box-shadow: 0 4px 10px rgba(0, 0, 0, 0.2); /* Bayangan untuk efek terangkat */
  border-bottom-left-radius: 10px;
  border-bottom-right-radius: 10px;
}
header h1 {
  margin: 0;
  font-weight: 600;
  letter-spacing: 1px;
}
main {
  flex-grow: 1;
  width: 100%;
  display: flex;
  justify-content: center;
  align-items: flex-start; /* Mengatur item ke atas */
  padding-bottom: 20px; /* Ruang di bawah container chat */
}
</style>
