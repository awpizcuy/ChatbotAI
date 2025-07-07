<template>
  <div id="chat-container">
    <div id="chat-messages">
      <div v-for="(msg, index) in messages" :key="index" :class="['message', msg.sender + '-message']">
        {{ msg.text }}
      </div>
    </div>
    <form @submit.prevent="sendMessage" id="chat-input-form">
      <input type="text" v-model="userInput" id="user-input" placeholder="Ketik pesan Anda..." :disabled="isLoading" />
      <button type="submit" id="send-button" :disabled="isLoading">
        {{ isLoading ? 'Mengirim...' : 'Kirim' }}
      </button>
    </form>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import axios from 'axios';
import type { Ref } from 'vue'; // Pastikan ini ada

// PENTING: SESUAIKAN URL BACKEND .NET ANDA DI SINI
const BACKEND_API_URL = 'http://localhost:5225/api/Chat';

// --- DEFINISI TIPE (INTERFACE) ---
interface ChatMessage {
  role: 'user' | 'assistant' | 'system';
  content: string;
}

interface UIMessage {
  text: string;
  sender: 'user' | 'ai';
}
// --- AKHIR DEFINISI TIPE ---

const userInput: Ref<string> = ref('');
const messages: Ref<UIMessage[]> = ref([]);
const isLoading: Ref<boolean> = ref(false);
const chatHistory: Ref<ChatMessage[]> = ref([]); // Pastikan ini 'const' dan memiliki tipe eksplisit

const addMessageToChat = (text: string, sender: 'user' | 'ai') => {
  messages.value.push({ text, sender });
  setTimeout(() => {
    const chatMessagesDiv = document.getElementById('chat-messages');
    if (chatMessagesDiv) {
      chatMessagesDiv.scrollTop = chatMessagesDiv.scrollHeight;
    }
  }, 50);
};

const sendMessage = async () => {
  const message = userInput.value.trim();
  if (message === '') return;

  addMessageToChat(message, 'user');
  userInput.value = '';
  isLoading.value = true;

  try {
    const response = await axios.post(BACKEND_API_URL, {
      message: message,
      history: chatHistory.value
    });

    if (response.data && response.data.reply && response.data.history) {
      const reply: string = response.data.reply;
      const newHistory: ChatMessage[] = response.data.history;

      addMessageToChat(reply, 'ai');
      chatHistory.value = newHistory;
    } else {
      console.error('Respons backend tidak valid:', response.data);
      addMessageToChat('Maaf, respons AI tidak lengkap.', 'ai');
    }

  } catch (error: unknown) { // <--- PASTIKSAN INI 'unknown' BUKAN 'any'
    console.error('Error sending message:', error);
    if (axios.isAxiosError(error)) { // Pastikan Anda punya axios versi yang mendukung isAxiosError
      if (error.response) {
        console.error('Data Error Respon:', error.response.data);
        addMessageToChat(`Error dari server: ${error.response.data?.error || error.response.statusText || 'Kesalahan tidak dikenal.'}`, 'ai');
      } else if (error.request) {
        addMessageToChat('Gagal terhubung ke server backend. Pastikan server berjalan, URL benar, dan CORS dikonfigurasi.', 'ai');
      } else {
        addMessageToChat(`Terjadi kesalahan saat menyiapkan permintaan: ${error.message}`, 'ai');
      }
    } else if (error instanceof Error) {
        addMessageToChat(`Terjadi kesalahan tak terduga: ${error.message}`, 'ai');
    } else {
        addMessageToChat(`Terjadi kesalahan yang sangat tidak terduga.`, 'ai');
    }
  } finally {
    isLoading.value = false;
  }
};

onMounted(() => {
  addMessageToChat("Halo! Ada yang bisa saya bantu?", "ai");
});
</script>

<style scoped>
/* Styling CSS dasar untuk chatbot */
#chat-container {
    max-width: 600px;
    margin: 20px auto;
    background-color: #fff;
    border-radius: 8px;
    box-shadow: 0 2px 10px rgba(0,0,0,0.1);
    display: flex;
    flex-direction: column;
    height: 70vh; /* Tinggi container chat */
    min-height: 400px; /* Tinggi minimum */
    overflow: hidden;
}
#chat-messages {
    flex-grow: 1; /* Agar mengambil sisa ruang vertikal */
    padding: 15px;
    overflow-y: auto; /* Bisa digulir jika pesan banyak */
    border-bottom: 1px solid #eee;
    display: flex;
    flex-direction: column; /* Pesan akan tersusun vertikal */
}
.message {
    margin-bottom: 10px;
    padding: 8px 12px;
    border-radius: 5px;
    max-width: 80%; /* Lebar maksimum pesan */
}
.user-message {
    background-color: #007bff;
    color: white;
    align-self: flex-end; /* Pesan user ke kanan */
    margin-left: auto; /* Memastikan pesan user menempel ke kanan */
}
.ai-message {
    background-color: #e2e6ea;
    color: #333;
    align-self: flex-start; /* Pesan AI ke kiri */
}
#chat-input-form {
    display: flex;
    padding: 15px;
    border-top: 1px solid #eee;
}
#user-input {
    flex-grow: 1;
    padding: 10px;
    border: 1px solid #ddd;
    border-radius: 5px;
    margin-right: 10px;
}
#send-button {
    padding: 10px 15px;
    background-color: #28a745;
    color: white;
    border: none;
    border-radius: 5px;
    cursor: pointer;
}
#send-button:hover {
    background-color: #218838;
}
#send-button:disabled {
    background-color: #999;
    cursor: not-allowed;
}
</style>
