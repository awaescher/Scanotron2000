# Scanotron 2000

Eine .NET 9 Console-Anwendung, die Microsoft Semantic Kernel verwendet, um mit **OpenAI-kompatiblen APIs** Headlines aus PDF-Seiten zu extrahieren.

## Features

- 🔍 **PDF-Verarbeitung**: Liest PDFs seitenweise mit iText7
- 🤖 **OpenAI-kompatible API**: Universelle Unterstützung für alle OpenAI-kompatiblen Endpoints
- 📄 **Headline-Extraktion**: Intelligente Erkennung der wichtigsten Überschriften pro Seite
- ⚙️ **Einfache Konfiguration**: Nur Endpoint, Modell und optional API-Key
- 🚀 **Automatische Modell-Erkennung**: Erkennt verfügbare Modelle automatisch vom Endpoint

## Unterstützte Plattformen

Alle Systeme mit **OpenAI-kompatiblen APIs**:
- **LM Studio** (Standard): Lokale Modelle über Port 1234
- **Ollama**: Lokale Modelle über Port 11434 
- **OpenAI**: Original OpenAI API
- **Azure OpenAI**: Microsoft Azure OpenAI Service
- **Alle anderen**: Jeder Server mit `/v1/models` und `/v1/chat/completions` Endpoints

## Installation

### Voraussetzungen

- .NET 9 SDK
- Ein OpenAI-kompatibler Server:
  - [LM Studio](https://lmstudio.ai/) (empfohlen für lokale Modelle)
  - [Ollama](https://ollama.ai/) (alternative für lokale Modelle)
  - OpenAI API Account
  - Oder jeder andere OpenAI-kompatible Service

### Klonen und Kompilieren

```bash
git clone <repository-url>
cd "Scanotron 2000"
dotnet build
```

## Verwendung

### Einfachste Verwendung (LM Studio)

```bash
# Automatische Modell-Erkennung
dotnet run document.pdf

# Mit spezifischem Modell
dotnet run document.pdf --model "llama-3.2-3b-instruct"
```

### Andere OpenAI-kompatible Services

```bash
# Ollama
dotnet run document.pdf --endpoint http://localhost:11434

# OpenAI (mit API Key)
dotnet run document.pdf --endpoint https://api.openai.com/v1 --apikey your-api-key

# Oder mit Umgebungsvariable
export OPENAI_API_KEY=your-api-key
dotnet run document.pdf --endpoint https://api.openai.com/v1

# Beliebiger OpenAI-kompatibler Server
dotnet run document.pdf --endpoint http://your-server:8080 --model your-model --apikey optional-key
```

### Vollständige Optionen

```bash
dotnet run <pdf-file> [options]

Arguments:
  <pdf-file>        Pfad zur PDF-Datei

Options:
  --model, -m       Modellname (wird automatisch erkannt wenn nicht angegeben)
  --endpoint, -e    API-Endpoint-URL [default: http://localhost:1234]
  --apikey, -k      API-Schlüssel (optional, kann auch über OPENAI_API_KEY env var)
  --help, -h        Hilfe anzeigen
```

## Konfiguration

### LM Studio (Standard)

1. [LM Studio herunterladen und installieren](https://lmstudio.ai/)
2. Ein Modell in LM Studio laden
3. LM Studio Server starten (standardmäßig auf Port 1234)
4. Scanotron2000 verwenden - erkennt Modelle automatisch!

```bash
dotnet run your-document.pdf
```

### Ollama

1. [Ollama installieren](https://ollama.ai/)
2. Ein Modell herunterladen: `ollama pull llama3`
3. Ollama starten: `ollama serve`
4. Mit Ollama verwenden:

```bash
dotnet run document.pdf --endpoint http://localhost:11434
```

### OpenAI

```bash
# Mit API Key als Parameter
dotnet run document.pdf --endpoint https://api.openai.com/v1 --apikey your-key --model gpt-4

# Oder mit Umgebungsvariable
export OPENAI_API_KEY=your-key
dotnet run document.pdf --endpoint https://api.openai.com/v1 --model gpt-4
```

### Beliebiger OpenAI-kompatibler Service

Jeder Service mit diesen Endpoints funktioniert:
- `GET /v1/models` - Liste verfügbarer Modelle
- `POST /v1/chat/completions` - Chat-Completion API

```bash
dotnet run document.pdf --endpoint http://your-api:port --model your-model
```

## Beispiel-Ausgabe

```
🔍 Discovering available models from http://localhost:1234...
🤖 Using first available model: llama-3.2-3b-instruct
💡 Other available models: gpt-4o-mini, claude-3-sonnet

🔍 Processing PDF: sample-document.pdf
🤖 Using model: llama-3.2-3b-instruct from http://localhost:1234

📄 Total pages: 5

📄 Page 1: Introduction to Machine Learning
📄 Page 2: Data Preprocessing Techniques
📄 Page 3: Neural Network Architectures
📄 Page 4: Training and Validation Methods
📄 Page 5: Conclusion and Future Work
```

## Technische Details

### Verwendete Pakete

- **Microsoft.SemanticKernel** (1.65.0): AI-Integration
- **iText7** (9.3.0): PDF-Verarbeitung
- **iText7.BouncyCastle-Adapter** (9.3.0): Kryptografie-Unterstützung für PDF-Verarbeitung
- **.NET 9**: Moderne C#-Features und Performance

### Architektur

- **Universal OpenAI-kompatibel**: Funktioniert mit jedem OpenAI-kompatiblen Service
- **Automatische Modell-Erkennung**: Erkennt verfügbare Modelle von allen Endpoints
- **Robuste Fehlerbehandlung**: Graceful Degradation bei API-Fehlern
- **Token-Management**: Automatische Textkürzung für Token-Limits
- **Einfache Konfiguration**: Nur 3 Parameter: Endpoint, Modell, API-Key (optional)

## Entwicklung

### DevExpress Integration

Um DevExpress DocumentProcessor zu verwenden (falls verfügbar):

1. DevExpress Lizenz und Pakete installieren
2. `iText7` durch DevExpress-Pakete ersetzen
3. PDF-Verarbeitungslogik anpassen

### Erweiterte Features

- Batch-Verarbeitung mehrerer PDFs
- Ausgabe in verschiedene Formate (JSON, XML, CSV)
- Konfigurierbare Prompts für verschiedene Anwendungsfälle
- OCR-Unterstützung für gescannte PDFs

## Troubleshooting

### Häufige Probleme

1. **"OPENAI_API_KEY environment variable is required"**
   - Umgebungsvariable für den gewählten Provider setzen

2. **"PDF file not found"**
   - Pfad zur PDF-Datei überprüfen

3. **"AI extraction failed"**
   - Provider-Verfügbarkeit prüfen (Ollama läuft, API-Keys gültig)
   - Netzwerkverbindung überprüfen

### Debug-Modus

Für detailliertere Fehlerinformationen kann das Projekt im Debug-Modus gestartet werden:

```bash
dotnet run --configuration Debug document.pdf
```

## Lizenz

[Lizenz hier einfügen]

## Beitragen

Contributions sind willkommen! Bitte erstellen Sie einen Pull Request oder Issue für Vorschläge und Verbesserungen.