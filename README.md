# Scanotron 2000 🚀

[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**Scanotron 2000** is a AI-powered pagewise PDF processing tool that extracts, analyzes, and transforms PDF content using customizable AI prompts. Built with .NET 9 and Microsoft Semantic Kernel, it supports multiple AI providers and offers specialized processing modes for different document analysis needs.

## 🚀 Quick Start

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- An AI model server (LM Studio, Ollama, etc.) or OpenAI API access

### Basic Usage

```bash
# extract headlines from each page
dotnet run -- document.pdf --prompt headliner

# find logical document breaks to split up merged documents
dotnet run -- large-document.pdf --prompt pagebreaker

# Generate JSON metadata for each page
dotnet run -- report.pdf --prompt json-derulo
```

## 🔧 Configuration

### AI Provider Setup

#### LM Studio (Default)
```bash
# Start LM Studio and load a model
# Default endpoint: http://localhost:1234
dotnet run -- document.pdf --prompt headliner
```

#### Ollama
```bash
dotnet run -- document.pdf --prompt headliner --endpoint http://localhost:11434
```

#### OpenAI
```bash
dotnet run -- document.pdf --prompt headliner --endpoint https://api.openai.com --apikey your-api-key
```

#### Custom OpenAI-Compatible API
```bash
dotnet run -- document.pdf --prompt headliner --endpoint http://your-server:8080 --model your-model
```

### Command Line Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--prompt` | `-p` | Prompt name (required) | - |
| `--model` | `-m` | Model name | Auto-detected |
| `--endpoint` | `-e` | API endpoint URL | `http://localhost:1234` |
| `--apikey` | `-k` | API key | `API_KEY` env var |
| `--list-prompts` | - | List available prompts | - |
| `--help` | `-h` | Show help | - |

## 🛠️ Custom Prompts

Create custom prompts by adding YAML files to the `Prompts/` directory:

```yaml
name: your-custom-prompt
template_format: semantic-kernel
description: Your prompt description

input_variables:
  - name: pageText
    description: The text content of the page
    is_required: true

execution_settings:
  default:
    temperature: 0.7

template: |
  Your custom prompt template here.
  Use {{ $pageText }} to access the page content.
```

## 🔍 How It Works

1. **PDF Text Extraction**: Uses iText7 to extract text content from PDF pages
2. **AI Processing**: Sends extracted text to AI models via OpenAI-compatible APIs
3. **Prompt Application**: Applies specialized prompts using Microsoft Semantic Kernel
4. **Result Output**: Returns processed results based on the selected prompt
