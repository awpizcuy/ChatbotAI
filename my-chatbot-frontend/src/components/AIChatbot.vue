<template>
  <div id="chat-container">
    <div id="chat-header">
      <div class="status-indicator"></div>
      <h3>AI Assistant</h3>
    </div>

    <div id="chat-messages">
      <div v-for="msg in messages" :key="msg.id" :class="['message', msg.sender + '-message']">
        <div v-if="msg.sender === 'ai'" v-html="renderMarkdown(msg.text)" class="markdown-content"></div>
        <template v-else>{{ msg.text }}</template>

        <div v-if="msg.sender === 'ai'" class="feedback-actions">
           <button @click.stop="handleFeedback(msg.id, 'like')" class="feedback-btn" :class="{ 'active like': msg.feedback === 'like' }" title="Jawaban membantu">
             <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" width="18" height="18"><path d="M1 8.25a1.25 1.25 0 1 1 2.5 0v7.5a1.25 1.25 0 1 1-2.5 0v-7.5ZM18.5 9.25a1.5 1.5 0 0 0-1.5-1.5h-5.553a.75.75 0 0 1-.723-.553L9.7 4.957A1.5 1.5 0 0 0 8.277 4H5.25a1.5 1.5 0 0 0-1.5 1.5v10.5a1.5 1.5 0 0 0 1.5 1.5h9.25a1.5 1.5 0 0 0 1.425-1.087l1.385-5.25a1.5 1.5 0 0 0-1.31-1.913Z" /></svg>
          </button>
          <button @click.stop="handleFeedback(msg.id, 'dislike')" class="feedback-btn" :class="{ 'active dislike': msg.feedback === 'dislike' }" title="Jawaban tidak membantu">
             <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" width="18" height="18"><path d="M19 11.75a1.25 1.25 0 1 1-2.5 0v-7.5a1.25 1.25 0 1 1 2.5 0v7.5ZM1.5 10.75a1.5 1.5 0 0 0 1.5 1.5h5.553a.75.75 0 0 1 .723.553l1.024 2.296a1.5 1.5 0 0 0 1.423 1.051h2.5a1.5 1.5 0 0 0 1.5-1.5V4.5a1.5 1.5 0 0 0-1.5-1.5h-9.25a1.5 1.5 0 0 0-1.425 1.087l-1.385 5.25a1.5 1.5 0 0 0 1.31-1.913Z" /></svg>
          </button>
        </div>
      </div>
      <div v-if="isLoading" class="message ai-message typing-indicator">
        <span></span>
        <span></span>
        <span></span>
      </div>
    </div>

    <form @submit.prevent="sendMessage" id="chat-input-form">
      <input type="text" v-model="userInput" id="user-input" placeholder="Ketik pesan Anda..." :disabled="isLoading" />
      <button type="submit" id="send-button" :disabled="isLoading">
        <div v-if="isLoading" class="loader"></div>
        <svg v-else xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="24" height="24" fill="currentColor">
          <path d="M2.01 21L23 12 2.01 3 2 10l15 2-15 2z"></path>
        </svg>
      </button>
    </form>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import axios from 'axios';
import type { Ref } from 'vue';
import MarkdownIt from 'markdown-it';

// PASTIKAN INI SESUAI DENGAN PORT BACKEND .NET ANDA
const BACKEND_API_URL = 'http://localhost:5225/api/Chat';

// --- DEFINISI TIPE (INTERFACE) ---
interface ChatMessage {
  role: 'user' | 'assistant' | 'system';
  content: string;
}
interface UIMessage {
  id: number;
  text: string;
  sender: 'user' | 'ai';
  feedback?: 'like' | 'dislike' | null;
}

// --- DEKLARASI DATA REAKTIF ---
const userInput: Ref<string> = ref('');
const messages: Ref<UIMessage[]> = ref([]);
const isLoading: Ref<boolean> = ref(false);
const chatHistory: Ref<ChatMessage[]> = ref([]);
const messageIdCounter = ref(0);

// --- INISIALISASI MARKDOWN-IT ---
const md = new MarkdownIt();

// Fungsi untuk menambahkan pesan ke UI
const addMessageToChat = (text: string, sender: 'user' | 'ai') => {
  messageIdCounter.value++;
  messages.value.push({
    id: messageIdCounter.value,
    text,
    sender,
    feedback: null
  });
  setTimeout(() => {
    const chatMessagesDiv = document.getElementById('chat-messages');
    if (chatMessagesDiv) {
      chatMessagesDiv.scrollTop = chatMessagesDiv.scrollHeight;
    }
  }, 50);
};

// Fungsi untuk merender teks Markdown ke HTML
const renderMarkdown = (markdownText: string): string => {
  return md.render(markdownText);
};

// ==================================================================
// === FUNGSI INI TELAH DIPERBARUI UNTUK MENGIRIM DATA KE BACKEND ===
// ==================================================================
const handleFeedback = async (messageId: number, feedbackType: 'like' | 'dislike') => {
  // 1. Cari pesan yang akan diberi feedback
  const messageIndex = messages.value.findIndex(m => m.id === messageId);
  if (messageIndex === -1) {
    console.error(`Pesan dengan ID ${messageId} tidak ditemukan.`);
    return;
  }

  const message = messages.value[messageIndex];
  const originalFeedback = message.feedback;

  // 2. Perbarui tampilan UI secara optimis
  message.feedback = message.feedback === feedbackType ? null : feedbackType;

  // 3. Siapkan data untuk dikirim
  const reversedMessages = [...messages.value].reverse();
  const reversedIndex = reversedMessages.findIndex(m => m.id < messageId && m.sender === 'user');
  const userMessageIndex = reversedIndex !== -1 ? messages.value.length - 1 - reversedIndex : -1;
  const userQuestion = userMessageIndex > -1 ? messages.value[userMessageIndex].text : '';

  // 4. Lakukan pemanggilan API ke backend di dalam blok try...catch
  try {
    console.log("Mencoba mengirim feedback ke backend...");

    const response = await axios.post('http://localhost:5225/api/Feedback', {
      conversationHistory: chatHistory.value,
      userQuestion: userQuestion,
      aiResponse: message.text,
      rating: message.feedback
    });

    console.log("Feedback berhasil dikirim:", response.data);

    // Jika di-dislike dan backend memberikan jawaban baru, perbarui UI
    if (feedbackType === 'dislike' && response.data.newReply) {
      messages.value[messageIndex].text = response.data.newReply;
      messages.value[messageIndex].feedback = null;
    }

  } catch (error) {
    console.error("GAGAL mengirim feedback:", error);
    // Jika gagal, kembalikan tampilan UI ke state semula
    messages.value[messageIndex].feedback = originalFeedback;
  }
};
// ==================================================================
// === AKHIR PERUBAHAN FUNGSI                                     ===
// ==================================================================


// Fungsi untuk mengirim pesan ke backend
const sendMessage = async () => {
  const message = userInput.value.trim();
  if (message === '' || isLoading.value) return;

  addMessageToChat(message, 'user');
  userInput.value = '';
  isLoading.value = true;

  setTimeout(() => {
    const chatMessagesDiv = document.getElementById('chat-messages');
    if (chatMessagesDiv) {
      chatMessagesDiv.scrollTop = chatMessagesDiv.scrollHeight;
    }
  }, 100);

  try {
    const response = await axios.post(BACKEND_API_URL, {
      message: message,
      history: chatHistory.value
    });

    if (response.data && response.data.reply && response.data.history) {
      const reply: string = response.data.reply;
      const newHistory: ChatMessage[] = response.data.history;

      isLoading.value = false;
      addMessageToChat(reply, 'ai');
      chatHistory.value = newHistory;

    } else {
      isLoading.value = false;
      console.error('Respons backend tidak valid:', response.data);
      addMessageToChat('Maaf, respons AI tidak lengkap atau tidak valid.', 'ai');
    }

  } catch (error: unknown) {
    isLoading.value = false;
    console.error('Error sending message:', error);
    if (axios.isAxiosError(error)) {
      if (error.response) {
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
  }
};

// Pesan sambutan awal
onMounted(() => {
  addMessageToChat("Halo! Saya adalah asisten AI Anda. Ada yang bisa saya bantu?", "ai");
});
</script>

<style scoped>
/* Menggunakan font yang lebih modern jika tersedia */
@import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap');

#chat-container {
  max-width: 800px;
  width: 100%;
  margin: 30px auto;
  background-color: #ffffff;
  border-radius: 24px;
  box-shadow: 0 20px 50px -10px rgba(0, 0, 0, 0.1);
  display: flex;
  flex-direction: column;
  height: 90vh;
  min-height: 600px;
  overflow: hidden;
  border: 1px solid #e5e7eb;
  font-family: 'Inter', sans-serif;
}

/* === HEADER === */
#chat-header {
  display: flex;
  align-items: center;
  padding: 16px 24px;
  background-color: #f9fafb;
  border-bottom: 1px solid #e5e7eb;
  z-index: 10;
}
#chat-header h3 {
  margin: 0;
  font-size: 1.1em;
  font-weight: 600;
  color: #111827;
}
.status-indicator {
  width: 10px;
  height: 10px;
  background-color: #22c55e; /* Hijau = Online */
  border-radius: 50%;
  margin-right: 12px;
  box-shadow: 0 0 8px #22c55e;
}

#chat-messages {
  flex-grow: 1;
  padding: 24px;
  overflow-y: auto;
  background-color: #f9fafb;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

/* Scrollbar styling */
#chat-messages::-webkit-scrollbar {
  width: 6px;
}
#chat-messages::-webkit-scrollbar-track {
  background: transparent;
}
#chat-messages::-webkit-scrollbar-thumb {
  background: #d1d5db;
  border-radius: 10px;
}
#chat-messages::-webkit-scrollbar-thumb:hover {
  background: #9ca3af;
}

.message {
  padding: 14px 20px;
  border-radius: 20px;
  max-width: 75%;
  line-height: 1.6;
  font-size: 0.95em;
  word-wrap: break-word;
  opacity: 0;
  transform: translateY(20px);
  animation: slide-in 0.5s cubic-bezier(0.25, 1, 0.5, 1) forwards;
  transition: transform 0.3s ease, box-shadow 0.3s ease;
}

.message:hover {
  transform: translateY(-4px) scale(1.02);
  box-shadow: 0 8px 20px -5px rgba(0, 0, 0, 0.1);
  cursor: pointer;
}

@keyframes slide-in {
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.user-message {
  background: linear-gradient(100deg, #4f46e5 0%, #7c3aed 100%);
  color: white;
  align-self: flex-end;
  margin-left: auto;
  border-bottom-right-radius: 5px;
}
.ai-message {
  background: #e5e7eb;
  color: #1f2937;
  align-self: flex-start;
  border-bottom-left-radius: 5px;
}

.feedback-actions {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 12px;
  border-top: 1px solid #dde1e7;
  padding-top: 10px;
}

.feedback-btn {
  background-color: transparent;
  border: none;
  padding: 4px;
  border-radius: 50%;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #6b7280;
  transition: all 0.2s ease;
}

.feedback-btn:hover {
  background-color: #f0f2f5;
}

.feedback-btn.active.like {
  color: #4f46e5;
  background-color: #eef2ff;
}

.feedback-btn.active.dislike {
  color: #ef4444;
  background-color: #fee2e2;
}

.typing-indicator {
    display: flex;
    align-items: center;
    gap: 5px;
    padding: 18px 20px;
}
.typing-indicator span {
    width: 8px;
    height: 8px;
    background-color: #9ca3af;
    border-radius: 50%;
    animation: bounce 1.4s infinite ease-in-out both;
}
.typing-indicator span:nth-of-type(1) { animation-delay: -0.32s; }
.typing-indicator span:nth-of-type(2) { animation-delay: -0.16s; }

@keyframes bounce {
    0%, 80%, 100% { transform: scale(0); }
    40% { transform: scale(1.0); }
}

.ai-message :deep(p) { margin-top: 0; margin-bottom: 0.5em; }
.ai-message :deep(p:last-child) { margin-bottom: 0; }
.ai-message :deep(strong) { font-weight: 600; color: #111827; }
.ai-message :deep(ul), .ai-message :deep(ol) { padding-left: 20px; margin: 0.5em 0; }
.ai-message :deep(h1), .ai-message :deep(h2), .ai-message :deep(h3) { margin-top: 1em; margin-bottom: 0.5em; font-weight: 600; color: #111827; }
.ai-message :deep(pre) {
    background-color: #1f2937;
    color: #e5e7eb;
    border-radius: 8px;
    padding: 12px 16px;
    overflow-x: auto;
    font-family: 'Fira Code', 'Consolas', monospace;
    font-size: 0.9em;
    margin: 1em 0;
}
.ai-message :deep(code) {
    background-color: rgba(0,0,0,0.1);
    border-radius: 4px;
    padding: 2px 6px;
    font-family: 'Fira Code', 'Consolas', monospace;
    font-size: 0.9em;
}

#chat-input-form {
  display: flex;
  align-items: center;
  padding: 16px 24px;
  border-top: 1px solid #e5e7eb;
  background-color: #ffffff;
  gap: 12px;
}

#user-input {
  flex-grow: 1;
  padding: 12px 20px;
  border: 1px solid #d1d5db;
  border-radius: 9999px;
  font-size: 1em;
  transition: all 0.2s ease-in-out;
  background-color: #f9fafb;
}
#user-input:focus {
  border-color: #4f46e5;
  background-color: #ffffff;
  box-shadow: 0 0 0 3px rgba(79, 70, 229, 0.2);
  outline: none;
}
#user-input:disabled {
  background-color: #e5e7eb;
  cursor: not-allowed;
}

#send-button {
  flex-shrink: 0;
  width: 48px;
  height: 48px;
  background: linear-gradient(100deg, #4f46e5 0%, #6366f1 100%);
  color: white;
  border: none;
  border-radius: 50%;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.3s cubic-bezier(0.25, 1, 0.5, 1);
  box-shadow: 0 4px 10px -2px rgba(79, 70, 229, 0.4);
}
#send-button:hover:not(:disabled) {
  background: linear-gradient(100deg, #4338ca 0%, #4f46e5 100%);
  transform: translateY(-2px);
  box-shadow: 0 6px 15px -3px rgba(79, 70, 229, 0.5);
}
#send-button:active:not(:disabled) {
  transform: translateY(0);
  box-shadow: 0 2px 5px -1px rgba(79, 70, 229, 0.5);
}
#send-button:disabled {
  background: #9ca3af;
  cursor: not-allowed;
  box-shadow: none;
}

.loader {
    width: 20px;
    height: 20px;
    border: 3px solid rgba(255, 255, 255, 0.3);
    border-top-color: #ffffff;
    border-radius: 50%;
    animation: spin 1s linear infinite;
}
@keyframes spin {
    to { transform: rotate(360deg); }
}

@media (max-width: 768px) {
  #chat-container {
    height: 100vh;
    width: 100%;
    margin: 0;
    border-radius: 0;
    border: none;
  }
  .message {
    max-width: 85%;
    font-size: 0.9em;
  }
}
</style>
