<template>
  <div id="chat-container">
    <div id="chat-messages">
      <div v-for="(msg, index) in messages" :key="index" :class="['message', msg.sender + '-message']">
        <div v-if="msg.sender === 'ai'" v-html="renderMarkdown(msg.text)" class="markdown-content"></div>
        <template v-else>{{ msg.text }}</template> </div>
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
import type { Ref } from 'vue';
import MarkdownIt from 'markdown-it'; // <--- UBAH IMPOR INI: Hapus MarkdownItVue, tambahkan MarkdownIt

// Tidak perlu mengimpor CSS dari markdown-it-vue lagi:
// import 'markdown-it-vue/dist/markdown-it-vue.css'; // <--- HAPUS BARIS INI

const BACKEND_API_URL = 'http://localhost:5225/api/Chat'; // Pastikan ini sesuai dengan port backend .NET Anda

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
const chatHistory: Ref<ChatMessage[]> = ref([]);

// --- INISIALISASI MARKDOWN-IT ---
const md = new MarkdownIt(); // Buat instance MarkdownIt
// -------------------------------

const addMessageToChat = (text: string, sender: 'user' | 'ai') => {
  messages.value.push({ text, sender });
  setTimeout(() => {
    const chatMessagesDiv = document.getElementById('chat-messages');
    if (chatMessagesDiv) {
      chatMessagesDiv.scrollTop = chatMessagesDiv.scrollHeight;
    }
  }, 50);
};

// --- FUNGSI BARU UNTUK MERENDER MARKDOWN ---
const renderMarkdown = (markdownText: string): string => {
  return md.render(markdownText); // Menggunakan instance MarkdownIt untuk merender
};
// ------------------------------------------

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

      addMessageToChat(reply, 'ai'); // 'reply' dari AI (teks Markdown) dikirim ke sini
      chatHistory.value = newHistory;

    } else {
      console.error('Respons backend tidak valid:', response.data);
      addMessageToChat('Maaf, respons AI tidak lengkap.', 'ai');
    }

  } catch (error: unknown) {
    console.error('Error sending message:', error);
    if (axios.isAxiosError(error)) {
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
/* Styling CSS dasar untuk chatbot - DIUBAH UNTUK TAMPILAN LEBIH BAIK */
#chat-container {
    max-width: 750px;
    width: 95%;
    margin: 40px auto;
    background-color: #ffffff;
    border-radius: 20px;
    box-shadow: 0 15px 45px rgba(0,0,0,0.1);
    display: flex;
    flex-direction: column;
    height: 85vh;
    min-height: 550px;
    overflow: hidden;
    border: none;
    font-family: 'Poppins', sans-serif;
}

#chat-messages {
    flex-grow: 1;
    padding: 25px;
    overflow-y: auto;
    border-bottom: 1px solid #e0e0e0;
    display: flex;
    flex-direction: column;
    gap: 12px;
    background-color: #f5f7fa;
}

/* Scrollbar styling untuk tampilan yang lebih rapi */
#chat-messages::-webkit-scrollbar {
  width: 8px;
}
#chat-messages::-webkit-scrollbar-track {
  background: #eef0f3;
  border-radius: 10px;
}
#chat-messages::-webkit-scrollbar-thumb {
  background: #cdd2d7;
  border-radius: 10px;
}
#chat-messages::-webkit-scrollbar-thumb:hover {
  background: #b0b5ba;
}

.message {
    padding: 14px 20px;
    border-radius: 25px;
    max-width: 70%;
    line-height: 1.6;
    font-size: 0.98em;
    word-wrap: break-word;
    box-shadow: 0 2px 8px rgba(0,0,0,0.08);
    transition: all 0.2s ease-in-out;
}
.message:hover {
    transform: translateY(-2px);
    box-shadow: 0 4px 12px rgba(0,0,0,0.1);
}

.user-message {
    background: linear-gradient(135deg, #6c5ce7 0%, #8d7fe6 100%);
    color: white;
    align-self: flex-end;
    margin-left: auto;
    border-bottom-right-radius: 8px;
}
.ai-message {
    background: linear-gradient(135deg, #e0e6ed 0%, #f0f2f5 100%);
    color: #333;
    align-self: flex-start;
    border-bottom-left-radius: 8px;
}

/* --- TAMBAHAN STYLING UNTUK KONTEN MARKDOWN YANG DIRENDER (menggunakan elemen HTML standar) --- */
/* Karena kita pakai v-html, kita target elemen HTML yang dihasilkan markdown-it */
.ai-message p { /* Target paragraf */
    margin-top: 0;
    margin-bottom: 0.5em;
}
.ai-message strong { /* Gaya untuk bold */
    font-weight: 700;
    color: #2c3e50;
}
.ai-message em { /* Gaya untuk italic */
    font-style: italic;
    color: #555;
}
.ai-message ul, .ai-message ol { /* Gaya untuk daftar */
    margin-left: 20px;
    padding-left: 0;
    list-style-position: inside; /* Agar bullet/angka di dalam padding */
    margin-top: 5px;
    margin-bottom: 5px;
}
.ai-message li {
    margin-bottom: 3px;
}
.ai-message h1, .ai-message h2, .ai-message h3 { /* Gaya untuk judul */
    font-weight: 600;
    margin-top: 15px;
    margin-bottom: 10px;
    color: #2c3e50;
    line-height: 1.2;
}
.ai-message pre { /* Gaya untuk blok kode */
    background-color: rgba(0,0,0,0.05);
    border-radius: 8px;
    padding: 10px 15px;
    overflow-x: auto;
    font-family: 'Fira Code', 'Consolas', monospace;
    font-size: 0.85em;
    margin-top: 10px;
    margin-bottom: 10px;
    white-space: pre-wrap;
    word-break: break-all;
}
.ai-message code { /* Gaya untuk inline code */
    background-color: rgba(0,0,0,0.08);
    border-radius: 4px;
    padding: 2px 4px;
    font-family: 'Fira Code', 'Consolas', monospace;
    font-size: 0.85em;
}
/* --- AKHIR STYLING MARKDOWN --- */

#chat-input-form {
    display: flex;
    padding: 20px 25px;
    border-top: 1px solid #e0e0e0;
    background-color: #ffffff;
    gap: 15px;
}
#user-input {
    flex-grow: 1;
    padding: 14px 20px;
    border: 1px solid #dcdcdc;
    border-radius: 30px;
    font-size: 1.05em;
    transition: all 0.3s ease;
    box-shadow: inset 0 1px 3px rgba(0,0,0,0.05);
}
#user-input:focus {
    border-color: #6c5ce7;
    box-shadow: 0 0 0 4px rgba(108, 92, 231, 0.2);
    outline: none;
}
#send-button {
    padding: 14px 30px;
    background: linear-gradient(135deg, #6c5ce7 0%, #4a3abf 100%);
    color: white;
    border: none;
    border-radius: 30px;
    cursor: pointer;
    font-size: 1.05em;
    font-weight: 600;
    transition: all 0.3s ease;
    box-shadow: 0 5px 15px rgba(108, 92, 231, 0.3);
}
#send-button:hover {
    background: linear-gradient(135deg, #4a3abf 0%, #3a2b9e 100%);
    box-shadow: 0 8px 20px rgba(108, 92, 231, 0.4);
    transform: translateY(-3px);
}
#send-button:active {
    background: #3a2b9e;
    box-shadow: 0 2px 5px rgba(108, 92, 231, 0.5);
    transform: translateY(0);
}
#send-button:disabled {
    background: linear-gradient(135deg, #cccccc 0%, #999999 100%);
    cursor: not-allowed;
    box-shadow: none;
    transform: none;
}

/* Responsifitas dasar */
@media (max-width: 768px) {
  #chat-container {
    height: 95vh;
    margin: 10px auto;
    border-radius: 10px;
  }
  #chat-input-form {
    flex-direction: column;
    padding: 15px;
    gap: 10px;
  }
  #user-input {
    margin-right: 0;
    margin-bottom: 0;
    border-radius: 20px;
  }
  #send-button {
    width: 100%;
    border-radius: 20px;
  }
  .message {
      max-width: 85%;
      font-size: 0.9em;
      padding: 10px 15px;
  }
}
</style>
