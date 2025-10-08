using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text.Json;

namespace Scanotron;

public class ModelInfo
{
	public string Id { get; set; } = string.Empty;
	public string Object { get; set; } = string.Empty;
}

public class ModelsResponse
{
	public string Object { get; set; } = string.Empty;
	public List<ModelInfo> Data { get; set; } = new();
}

class Program
{
	static async Task<int> Main(string[] args)
	{
		// Simple argument parsing
		if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
		{
			ShowHelp();
			return 0;
		}

		if (args[0] == "--list-prompts")
		{
			ListAvailablePrompts();
			return 0;
		}

		string pdfPath = args[0];
		string? specifiedModel = GetArgValue(args, "--model", "-m");
		string? promptName = GetArgValue(args, "--prompt", "-p");
		string? outputFormat = GetArgValue(args, "--format", "-f");
		bool noIntro = HasFlag(args, "--no-intro");
		string endpoint = GetArgValue(args, "--endpoint", "-e") ?? "http://localhost:1234";
		string? apiKey = GetArgValue(args, "--apikey", "-k") ?? Environment.GetEnvironmentVariable("API_KEY");

		if (!File.Exists(pdfPath))
		{
			Console.WriteLine($"❌ Error: PDF file '{pdfPath}' not found.");
			return 1;
		}

		if (string.IsNullOrEmpty(promptName))
		{
			Console.WriteLine("❌ Error: The --prompt parameter is required.");
			Console.WriteLine("💡 Example: scanotron document.pdf --prompt headliner");
			return 1;
		}

		// Get available models or use specified model
		string model;
		if (string.IsNullOrEmpty(specifiedModel))
		{
			if (!noIntro)
			{
				Console.WriteLine($"🔍 Discovering available models from {endpoint}...");
			}
			var availableModels = await GetAvailableModelsAsync(endpoint, apiKey);

			if (availableModels.Count == 0)
			{
				Console.WriteLine($"❌ No models found at {endpoint}. Please specify a model with --model or start your model server.");
				return 1;
			}

			model = availableModels[0];

			if (!noIntro)
			{
				Console.WriteLine($"🤖 Using first available model: {model}");

				if (availableModels.Count > 1)
				{
					Console.WriteLine($"💡 Other available models: {string.Join(", ", availableModels.Skip(1))}");
				}
			}
		}
		else
		{
			model = specifiedModel;
		}

		await ProcessPdfAsync(new FileInfo(pdfPath), promptName, outputFormat, noIntro, model, endpoint, apiKey);
		return 0;
	}

	static void ShowHelp()
	{
		try
		{
			var kernel = CreateKernel("dummy-model", "http://localhost:1234", null);
			var availablePrompts = GetAvailablePromptsFromKernel(kernel);
			var promptsList = availablePrompts.Count > 0 ? string.Join(", ", availablePrompts) : "No prompts found";

			Console.WriteLine("Scanotron 2000 - AI-powered PDF headline extractor");
			Console.WriteLine();
			Console.WriteLine("Usage: scanotron <pdf-file> --prompt <prompt-name> [options]");
			Console.WriteLine();
			Console.WriteLine("Arguments:");
			Console.WriteLine("  <pdf-file>    Path to the PDF file to process");
			Console.WriteLine();
			Console.WriteLine("Options:");
			Console.WriteLine($"  --prompt, -p      Prompt name or direct prompt text - available: {promptsList}");
			Console.WriteLine("                    Can be a prompt name (e.g., 'headliner') or direct text (e.g., 'summarize this')");
			Console.WriteLine("  --format, -f      Output format template [default: 'Page {pageNumber}:\\n{answer}\\n']");
			Console.WriteLine("                    Variables: {pageNumber}, {pageCount}, {pageText}, {previousPageText}, {answer}");
			Console.WriteLine("                    Supports .NET formatting: {pageNumber:D3}, {pageCount:N0}, etc.");
			Console.WriteLine("  --no-intro        Suppress all informational output, only show results");
			Console.WriteLine("  --model, -m       Model name (auto-detected if not specified)");
			Console.WriteLine("  --endpoint, -e    API endpoint URL [default: http://localhost:1234]");
			Console.WriteLine("  --apikey, -k      API key (optional, can also use API_KEY env var)");
			Console.WriteLine("  --list-prompts    List all available prompts and their descriptions");
			Console.WriteLine("  --help, -h        Show this help");
			Console.WriteLine();
			Console.WriteLine("Examples:");
			Console.WriteLine("  # Basic usage");
			Console.WriteLine("  scanotron document.pdf --prompt headliner");
			Console.WriteLine("  scanotron document.pdf --prompt \"fasse zusammen\"");
			Console.WriteLine("  scanotron document.pdf --prompt headliner --format \"Seite {pageNumber}: {answer}\"");
			Console.WriteLine("  scanotron document.pdf --prompt headliner --no-intro");
			Console.WriteLine();
			Console.WriteLine("  # Advanced formatting");
			Console.WriteLine("  scanotron document.pdf --prompt headliner --format \"Page {pageNumber:D3}/{pageCount:D3}: {answer}\"");
			Console.WriteLine("  scanotron document.pdf --prompt headliner --format \"{pageNumber:D2}. {answer}\\n\"");
			Console.WriteLine();
			Console.WriteLine("  # Ollama");
			Console.WriteLine("  scanotron document.pdf --prompt headliner --endpoint http://localhost:11434");
			Console.WriteLine();
			Console.WriteLine("  # OpenAI");
			Console.WriteLine("  scanotron document.pdf --prompt headliner --endpoint https://api.openai.com --apikey your-key");
			Console.WriteLine();
			Console.WriteLine("  # Any OpenAI-compatible API");
			Console.WriteLine("  scanotron document.pdf --prompt headliner --endpoint http://your-server:8080 --model your-model");
			Console.WriteLine();
			Console.WriteLine("Note: When no model is specified, the first available model will be used automatically.");
		}
		catch
		{
			// Fallback if kernel creation fails
			Console.WriteLine("scanotron 2000 - AI-powered PDF headline extractor");
			Console.WriteLine();
			Console.WriteLine("Usage: scanotron <pdf-file> --prompt <prompt-name> [options]");
			Console.WriteLine();
			Console.WriteLine("Arguments:");
			Console.WriteLine("  <pdf-file>    Path to the PDF file to process");
			Console.WriteLine();
			Console.WriteLine("Options:");
			Console.WriteLine("  --prompt, -p      Prompt name (required)");
			Console.WriteLine("  --format, -f      Output format template [default: 'Page {pageNumber}:\\n{answer}\\n']");
			Console.WriteLine("  --no-intro        Suppress all informational output, only show results");
			Console.WriteLine("  --model, -m       Model name (auto-detected if not specified)");
			Console.WriteLine("  --endpoint, -e    API endpoint URL [default: http://localhost:1234]");
			Console.WriteLine("  --apikey, -k      API key (optional, can also use API_KEY env var)");
			Console.WriteLine("  --list-prompts    List all available prompts and their descriptions");
			Console.WriteLine("  --help, -h        Show this help");
		}
	}

	static string? GetArgValue(string[] args, params string[] argNames)
	{
		for (int i = 0; i < args.Length - 1; i++)
		{
			if (argNames.Contains(args[i]))
			{
				return args[i + 1];
			}
		}
		return null;
	}

	static bool HasFlag(string[] args, params string[] flagNames)
	{
		return args.Any(arg => flagNames.Contains(arg));
	}

	static string FormatOutputString(string template, int pageNumber, int pageCount, string pageText, string previousPageText, string answer)
	{
		try
		{
			// First handle escape sequences
			template = template.Replace("\\n", "\n")
							 .Replace("\\t", "\t")
							 .Replace("\\r", "\r");

			// Use string.Format with indexed placeholders for .NET formatting support
			// This allows formats like {0:D3} for zero-padded numbers, {0:C} for currency, etc.
			var formattedString = string.Format(template
				.Replace("{pageNumber}", "{0}")
				.Replace("{pageCount}", "{1}")
				.Replace("{pageText}", "{2}")
				.Replace("{previousPageText}", "{3}")
				.Replace("{answer}", "{4}"),
				pageNumber, pageCount, pageText, previousPageText, answer);

			return formattedString;
		}
		catch (FormatException)
		{
			// Fallback to simple replacement if format string is invalid
			return template
				.Replace("{pageNumber}", pageNumber.ToString())
				.Replace("{pageCount}", pageCount.ToString())
				.Replace("{pageText}", pageText)
				.Replace("{previousPageText}", previousPageText)
				.Replace("{answer}", answer)
				.Replace("\\n", "\n")
				.Replace("\\t", "\t")
				.Replace("\\r", "\r");
		}
	}

	static List<string> GetAvailablePrompts()
	{
		try
		{
			var kernel = CreateKernel("dummy-model", "http://localhost:1234", null);
			return GetAvailablePromptsFromKernel(kernel);
		}
		catch
		{
			return new List<string>();
		}
	}

	static List<string> GetAvailablePromptsFromKernel(Kernel kernel)
	{
		try
		{
			var promptsPlugin = kernel.Plugins["Prompts"];
			return promptsPlugin.Select(f => f.Name).ToList();
		}
		catch
		{
			return new List<string>();
		}
	}

	static void ListAvailablePrompts()
	{
		Console.WriteLine("Available Prompts:");
		Console.WriteLine("==================");
		Console.WriteLine();

		try
		{
			var kernel = CreateKernel("dummy-model", "http://localhost:1234", null);
			var promptsPlugin = kernel.Plugins["Prompts"];

			if (!promptsPlugin.Any())
			{
				Console.WriteLine("❌ No prompt functions found in the Prompts plugin.");
				return;
			}

			foreach (var function in promptsPlugin)
			{
				Console.WriteLine($"📝 {function.Name}");
				Console.WriteLine($"   {function.Description ?? "No description available"}");
				Console.WriteLine();
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"❌ Error loading prompts: {ex.Message}");
		}
	}

	static async Task<List<string>> GetAvailableModelsAsync(string endpoint, string? apiKey)
	{
		try
		{
			using var httpClient = new HttpClient();
			httpClient.Timeout = TimeSpan.FromSeconds(10);

			// Always use OpenAI-compatible /v1/models endpoint
			string modelsUrl = $"{endpoint.TrimEnd('/')}/v1/models";

			// Add authentication header if API key is provided
			if (!string.IsNullOrEmpty(apiKey))
			{
				httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
			}

			var response = await httpClient.GetStringAsync(modelsUrl);

			// Parse OpenAI-compatible response format
			var modelsResponse = JsonSerializer.Deserialize<ModelsResponse>(response, new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			});

			return modelsResponse?.Data?.Select(m => m.Id).Where(id => !string.IsNullOrEmpty(id)).ToList() ?? new List<string>();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"⚠️  Could not fetch models from {endpoint}: {ex.Message}");
			return new List<string>();
		}
	}

	static async Task ProcessPdfAsync(FileInfo pdfFile, string promptName, string? outputFormat, bool noIntro, string model, string endpoint, string? apiKey)
	{
		try
		{
			if (!noIntro)
			{
				Console.WriteLine($"🔍 Processing PDF: {pdfFile.Name}");
				Console.WriteLine($"🤖 Using model: {model} from {endpoint}");
			}

			// Initialize Semantic Kernel with OpenAI-compatible API
			var kernel = CreateKernel(model, endpoint, apiKey);

			// Check if promptName is a known prompt or direct text
			bool isPromptName = false;
			try
			{
				var promptsPlugin = kernel.Plugins["Prompts"];
				isPromptName = promptsPlugin.Any(f => f.Name == promptName);
			}
			catch
			{
				// If we can't load prompts, assume it's direct text
				isPromptName = false;
			}

			if (!noIntro)
			{
				if (isPromptName)
				{
					Console.WriteLine($"📝 Using prompt template: {promptName}");
				}
				else
				{
					Console.WriteLine($"📝 Using direct prompt: \"{promptName}\"");
				}
			}

			// Set default output format if none provided
			if (string.IsNullOrEmpty(outputFormat))
			{
				outputFormat = "Page {pageNumber}:\n{answer}\n";
			}

			if (!noIntro)
			{
				Console.WriteLine();
			}

			// Extract text from each page
			using var pdfReader = new PdfReader(pdfFile.FullName);
			using var pdfDocument = new PdfDocument(pdfReader);

			int pageCount = pdfDocument.GetNumberOfPages();

			if (!noIntro)
			{
				Console.WriteLine($"📄 Total pages: {pageCount}");
				Console.WriteLine();
			}

			var previousPageText = string.Empty;

			for (int pageNumber = 1; pageNumber <= pageCount; pageNumber++)
			{
				try
				{
					// Extract text from current page
					var page = pdfDocument.GetPage(pageNumber);
					var strategy = new SimpleTextExtractionStrategy();
					var pageText = PdfTextExtractor.GetTextFromPage(page, strategy);

					if (string.IsNullOrWhiteSpace(pageText))
					{
						if (!noIntro)
						{
							Console.WriteLine($"📄 Page {pageNumber}: [No extractable text found]");
						}
						previousPageText = pageText; // Update for next iteration
						continue;
					}

					// Use AI to extract headline
					var result = await ProcessPage(kernel, promptName, isPromptName, pageText, pageNumber, pageCount, previousPageText);

					// Format output using the specified template with .NET string formatting support
					var formattedOutput = FormatOutputString(outputFormat, pageNumber, pageCount, pageText, previousPageText, result);

					Console.Write(formattedOutput);

					// Store current page text for next iteration
					previousPageText = pageText;
				}
				catch (Exception ex)
				{
					if (!noIntro)
					{
						Console.WriteLine($"❌ Error processing page {pageNumber}: {ex.Message}");
					}
				}
			}
		}
		catch (Exception ex)
		{
			if (!noIntro)
			{
				Console.WriteLine($"❌ Error processing PDF: {ex.Message}");
			}
		}
	}

	static Kernel CreateKernel(string model, string endpoint, string? apiKey)
	{
		var builder = Kernel.CreateBuilder();

		// Ensure endpoint has proper format for Semantic Kernel
		var baseUri = endpoint.TrimEnd('/');
		if (!baseUri.EndsWith("/v1"))
			baseUri += "/v1";

		// Always use OpenAI-compatible API
		builder.AddOpenAIChatCompletion(
			modelId: model,
			apiKey: apiKey ?? "-", // Use provided API key or placeholder for local servers
			endpoint: new Uri(baseUri));

		builder.Plugins.AddFromPromptDirectoryYaml("Prompts");

		return builder.Build();
	}

	static async Task<string> ProcessPage(Kernel kernel, string promptName, bool isPromptName, string pageText, int pageNumber, int pageCount, string previousPageText)
	{
		try
		{
			if (isPromptName)
			{
				// Use predefined prompt template
				var arguments = new KernelArguments()
				{
					["pageText"] = pageText,
					["pageNumber"] = pageNumber,
					["pageCount"] = pageCount,
					["previousPageText"] = previousPageText
				};

				// Get the function from the kernel and invoke it
				var function = kernel.Plugins.GetFunction("Prompts", promptName);
				var result = await kernel.InvokeAsync(function, arguments);
				var headline = result.GetValue<string>()?.Trim();

				return headline ?? string.Empty;
			}
			else
			{
				// Use direct prompt text
				var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
				var result = await chatCompletion.GetChatMessageContentAsync(promptName + "\n\n" + pageText);
				return result.Content?.Trim() ?? string.Empty;
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"⚠️  AI extraction failed for page {pageNumber}: {ex.Message}");
			return $"Page {pageNumber} Content (AI extraction failed)";
		}
	}
}
