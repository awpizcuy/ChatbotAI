# AI Chatbot with RAG & .NET 8

[![Vue.js](https://img.shields.io/badge/Vue.js-4FC08D?style=for-the-badge&logo=vuedotjs&logoColor=white)](https://vuejs.org/)
[![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![SQLite](https://img.shields.io/badge/SQLite-003B57?style=for-the-badge&logo=sqlite&logoColor=white)](https://www.sqlite.org/)
[![Ollama](https://img.shields.io/badge/Ollama-000000?style=for-the-badge&logo=ollama&logoColor=white)](https://ollama.com/)

Proyek ini adalah implementasi **AI Chatbot _full-stack_** yang cerdas dan interaktif, yang dirancang untuk menjawab pertanyaan berdasarkan konteks dokumen spesifik menggunakan teknik **Retrieval-Augmented Generation (RAG)**. Proyek ini menggabungkan _backend_ **ASP.NET Core 8 Web API** dan _frontend_ **Vue.js** untuk menciptakan pengalaman pengguna yang mulus.

## Fitur Utama

* **Retrieval-Augmented Generation (RAG)**: Chatbot dapat menjawab pertanyaan secara akurat dengan merujuk pada dokumen eksternal (`MODUL 1.txt`). Dokumen dipecah menjadi _chunks_ dan diubah menjadi vektor numerik (`embeddings`) menggunakan model **Ollama `nomic-embed-text`**.
* **Fleksibilitas Model AI**: Arsitektur yang modular memungkinkan penggunaan berbagai model AI. Secara _default_, proyek ini terintegrasi dengan model lokal **Ollama** (`gemma3`), namun juga mendukung API eksternal seperti **DeepSeek** dan **Meta** melalui konfigurasi.
* **Persistensi Data**: Setiap pertanyaan pengguna dan jawaban AI disimpan ke database **SQLite** menggunakan **Entity Framework Core**, memungkinkan pelacakan riwayat dan analisis lebih lanjut.
* **Antarmuka Pengguna Modern**: Frontend Vue.js menyediakan antarmuka yang bersih, responsif, dan user-friendly, lengkap dengan rendering Markdown, indikator "mengetik...", dan sistem feedback (suka/tidak suka).
* **Sistem Feedback**: Pengguna dapat memberikan penilaian (suka/tidak suka) pada respons AI, yang datanya dikirim ke _backend_ untuk tujuan peningkatan dan evaluasi performa model.

## Teknologi

### Backend (`MyChatbotBackend`)
* **Kerangka Kerja**: ASP.NET Core 8 Web API
* **Database**: SQLite
* **ORM**: Entity Framework Core
* **AI & RAG**: Ollama (model lokal `gemma3` dan `nomic-embed-text`), `System.Net.Http.Json`
* **Utilitas**: `HttpClient` untuk komunikasi API, `System.Text.Json.Serialization`

### Frontend (`my-chatbot-frontend`)
* **Kerangka Kerja**: Vue.js (dengan TypeScript)
* **Manajer Paket**: npm
* **Komunikasi API**: `axios`
* **Rendering Markdown**: `markdown-it`
* **Styling**: CSS

## Panduan Instalasi & Setup

Ikuti langkah-langkah berikut untuk menjalankan proyek ini di mesin lokal Anda.

### Prasyarat

* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* [Node.js](https://nodejs.org/en/download/) (disertai npm)
* [Ollama](https://ollama.com/) terpasang dan berjalan di sistem Anda.

### Langkah 1: Siapkan Ollama

Pastikan Ollama berjalan di port default (11434). Buka terminal dan _pull_ model-model yang diperlukan:
```bash
ollama pull gemma3
ollama pull nomic-embed-text
