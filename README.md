# Scanotron2000

Scanotron2000 is a PDF processing orchestrator that combines two powerful tools:
- [pdfbrrr](https://github.com/awaescher/pdfbrrr) - AI-powered PDF processor
- [split-happens](https://github.com/awaescher/split-happens) - PDF splitting tool

### How it works

1. Scanotron2000 takes a PDF file as input
2. It runs `pdfbrrr` on the PDF with the `split-happens` prompt to analyze the document structure and extract a page splitting pattern
3. It then runs `split-happens` with the original PDF and the pattern to split the document into logical parts
4. The resulting split PDFs are saved to the specified output directory

## Running precompiled executables

> [!WARNING]  
> Apple is very restrictive when it comes to opening apps from unverified developers. To be able to use this app, you need to manually allow the executable to be run.
>
> That means:
>   1. Run the app from Terminal
>   2. Dismiss the dialog that pops up
>   3. Head over to your settings → Privacy & Security → and hit "Open Anyway"
>   4. Run the app again from Terminal
>   5. Click on open anyway in the dialog that pops up
>   6. Enter your Mac password to confirm
>   7. Run the app a third time, this time it should work
>
> More details here: https://support.apple.com/en-us/guide/mac-help/mh40616/mac
> 
> The bad news: You need to do this not only once but three times: for **scanotron**, **pdfbrrr** and **split-happens**.
>
> Once you're through, this will last until you download a new version of these apps.

Download the latest executable from the releases page. Use framework dependent, if you have the [.NET 9.0 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) installed. Otherwise chose self-containing.

Run `scanotron <pdf-file>`

See [arguments](#arguments) for more flexibility like defining an AI endpoint, an AI model, the output directory and more. 

## Running from source code

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- Git (for submodules)

### Getting Started

#### Clone the repository

```bash
git clone --recurse-submodules https://github.com/your-username/Scanotron2000.git
cd Scanotron2000
```

#### Build the projects

```bash
# Build pdfbrrr
cd pdfbrrr
dotnet build -c Release
cd ..

# Build split-happens
cd split-happens
dotnet build -c Release
cd ..

# Build scanotron
cd scanotron
dotnet build -c Release
cd ..
```

#### Run the application

```bash
cd scanotron
dotnet run -- [PDF-file] [--output <output-directory>]
```

Example:
```bash
dotnet run -- ../sample.pdf --output ../output
```

### Usage

```
scanotron <PDF-file> [--output <output-directory>] [--model <model>] [--endpoint <endpoint>] [--apikey <apikey>] [--force] [--json]
scanotron -f <PDF-file> [-o <output-directory>] [-m <model>] [-e <endpoint>] [-k <apikey>] [--force] [--json]
```

#### Arguments

- `<PDF-file>` - Path to the PDF file to process
- `--output, -o` - Optional: Directory where output files will be saved (default: folder with the same name next to the input PDF)
- `--model, -m` - Optional: Model name to use for AI processing
- `--endpoint, -e` - Optional: API endpoint URL [default: http://localhost:1234]
- `--apikey, -k` - Optional: API key (can also use API_KEY env var)
- `--force` - Optional: Force regenerate pattern, ignore cached .brrr file
- `--json` - Optional: Output logs in JSON format for machine processing

#### Process

1. Runs pdfbrrr on the PDF with the 'split-happens' prompt to extract a page pattern
   (Pattern is cached in a .brrr file for future runs)
2. Runs split-happens on the PDF using the pattern to split the document

#### Logging Modes

**Human-readable mode (default):**
- Clear visual separation with section dividers
- Emoji icons for different log levels (✓, ℹ️, ⚠️, ❌)
- Structured summary box at the end
- Embedded JSON data for detailed information

**Machine-readable mode (--json):**
- Each log entry is a separate JSON line
- Includes timestamp, log level, message, and structured data
- Perfect for parsing and automation
- Fields: `timestamp`, `level`, `message`, `data`

### Example

```bash
# Process a PDF file and save results to default output directory
# (creates a [pdf-name]/ folder next to the PDF)
dotnet run -- document.pdf

# Process a PDF with explicit output directory
dotnet run -- /path/to/document.pdf --output /custom/output/path

# Force regenerate pattern (ignore cache)
dotnet run -- document.pdf --force

# Use machine-readable JSON output
dotnet run -- document.pdf --json

# Use custom AI model
dotnet run -- document.pdf --model gpt-4 --endpoint https://api.openai.com --apikey your-key
```

**Example with default output:**
- Input: `/home/user/documents/invoice.pdf`
- Output: `/home/user/documents/invoice-split/`
  - `invoice-split/invoice 1.pdf`
  - `invoice-split/invoice 2+3.pdf`
  - etc.

### Pattern Caching

When processing a PDF, scanotron saves the extracted pattern to a `.brrr` file next to the original PDF. On subsequent runs:
- If the `.brrr` file exists, scanotron will use the cached pattern instead of running pdfbrrr again
- This speeds up repeated processing of the same document
- Use `--force` to ignore the cache and regenerate the pattern

Use the `--force` flag to regenerate the pattern when:
- The original PDF has been modified
- You want to retry after fixing an LLM server issue
- Previous analysis had too many failed pages
- You want to use a different model or endpoint

## How it works internally

1. First, pdfbrrr is executed with the command:
   ```
   pdfbrrr [PDF-file] --prompt split-happens
   ```
   This analyzes the PDF and outputs a page splitting pattern.

2. Then, split-happens is executed with the command:
   ```
   split-happens --file [PDF-file] --pattern [pattern-from-pdfbrrr] --output [output-directory]
   ```
   This splits the PDF according to the pattern and saves the parts to the output directory.
