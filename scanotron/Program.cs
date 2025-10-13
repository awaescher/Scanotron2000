using System.Diagnostics;
using System.Text.Json;

namespace scanotron;

record CommandLineArguments(
	string? PdfFile,
	string? OutputDirectory,
	string? Model,
	string? Endpoint,
	string? ApiKey,
	bool Force
);

class Program
{
	private static bool _jsonOutput = false;

	static async Task<int> Main(string[] args)
	{
		// Check for help command
		if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
		{
			ShowHelp();
			return 0;
		}

		// Parse command line arguments
		var arguments = ParseArguments(args);

		// Validate required arguments
		if (string.IsNullOrEmpty(arguments.PdfFile))
		{
			LogError("PDF file parameter is required.");
			ShowHelp();
			return 1;
		}

		// Check if the PDF file exists
		if (!File.Exists(arguments.PdfFile))
		{
			LogError($"The file '{arguments.PdfFile}' was not found.");
			return 1;
		}

		try
		{
			// Set default output directory if not specified
			var outputDirectory = arguments.OutputDirectory;
			if (string.IsNullOrEmpty(outputDirectory))
			{
				// Create output directory next to the PDF file: [pdf-name]/
				var pdfDirectory = Path.GetDirectoryName(Path.GetFullPath(arguments.PdfFile)) ?? Directory.GetCurrentDirectory();
				var pdfNameWithoutExtension = Path.GetFileNameWithoutExtension(arguments.PdfFile);
				outputDirectory = Path.Combine(pdfDirectory, $"{pdfNameWithoutExtension}");
			}

			// Ensure output directory exists
			Directory.CreateDirectory(outputDirectory);

			// Generate .brrr file path (same name as PDF, but with .brrr extension)
			var brrrFilePath = Path.ChangeExtension(arguments.PdfFile, ".brrr") ?? arguments.PdfFile + ".brrr";
			string? pattern;

			// Step 1: Check if .brrr file already exists (unless --force is used)
			if (File.Exists(brrrFilePath) && !arguments.Force)
			{
				LogStep(1, "Using cached pattern");
				LogSuccess($"Found existing pattern file: {brrrFilePath}");
				LogInfo("Use --force to regenerate the pattern");
				pattern = File.ReadAllText(brrrFilePath).Trim();
				LogInfo($"Pattern from cache: {pattern}", new { pattern, source = "cache" });
			}
			else
			{
				// Execute pdfbrrr to get the pattern
				if (arguments.Force && File.Exists(brrrFilePath))
					LogStep(1, "Force regenerating pattern (ignoring cache)");
				else
					LogStep(1, "Running pdfbrrr to analyze the PDF");

				pattern = await RunPdfbrrrAsync(arguments.PdfFile, arguments.Model, arguments.Endpoint, arguments.ApiKey);

				if (string.IsNullOrEmpty(pattern))
				{
					LogError("pdfbrrr did not return a valid pattern.");
					return 1;
				}

				LogSuccess($"Pattern extracted from pdfbrrr: {pattern}", new { pattern, source = "pdfbrrr" });

				// Save pattern to .brrr file for future use
				try
				{
					File.WriteAllText(brrrFilePath, pattern);
					LogSuccess($"Pattern saved to: {brrrFilePath}");
				}
				catch (Exception ex)
				{
					LogWarning($"Could not save pattern file: {ex.Message}");
				}
			}

			// Step 2: Execute split-happens with the pattern
			LogStep(2, "Running split-happens to split the PDF");

			var splitResult = await RunSplitHappensAsync(arguments.PdfFile, pattern, outputDirectory);

			if (splitResult.success)
			{
				LogSummary("Processing Complete", new Dictionary<string, object>
				{
					{ "Input PDF", arguments.PdfFile },
					{ "Pattern", pattern },
					{ "Output Directory", outputDirectory },
					{ "Total Pages", splitResult.totalPages },
					{ "Files Created", splitResult.filesCreated }
				});
				return 0;
			}
			else
			{
				LogError("split-happens failed to process the PDF.");
				return 1;
			}
		}
		catch (Exception ex)
		{
			LogError($"Error processing the PDF file: {ex.Message}", new { exception = ex.GetType().Name, message = ex.Message });
			return 1;
		}
	}

	static void ShowHelp()
	{
		Console.WriteLine("Scanotron 2000 - Split PDFs with LLMs.");
		Console.WriteLine();
		Console.WriteLine("Usage:");
		Console.WriteLine("  scanotron <PDF-file> [--output <output-directory>] [--model <model>] [--endpoint <endpoint>] [--apikey <apikey>] [--force] [--json]");
		Console.WriteLine("  scanotron -f <PDF-file> [-o <output-directory>] [-m <model>] [-e <endpoint>] [-k <apikey>] [--force] [--json]");
		Console.WriteLine();
		Console.WriteLine("Arguments:");
		Console.WriteLine("  <PDF-file>              Path to the PDF file to process");
		Console.WriteLine("  --output, -o            Optional: Directory where output files will be saved");
		Console.WriteLine("                          Default: a folder with the same name next to the input PDF");
		Console.WriteLine("  --model, -m             Optional: Model name to use for AI processing");
		Console.WriteLine("  --endpoint, -e          Optional: API endpoint URL [default: http://localhost:1234]");
		Console.WriteLine("  --apikey, -k            Optional: API key (can also use API_KEY env var)");
		Console.WriteLine("  --force                 Optional: Force regenerate pattern, ignore cached .brrr file");
		Console.WriteLine("  --json                  Optional: Output logs in JSON format for machine processing");
		Console.WriteLine();
		Console.WriteLine("Process:");
		Console.WriteLine("  1. Runs pdfbrrr on the PDF with the 'split-happens' prompt to extract a page pattern");
		Console.WriteLine("     (Pattern is cached in a .brrr file for future runs)");
		Console.WriteLine("  2. Runs split-happens on the PDF using the pattern to split the document");
		Console.WriteLine();
		Console.WriteLine("Examples:");
		Console.WriteLine("  scanotron document.pdf");
		Console.WriteLine("  scanotron document.pdf --output ./my-output");
		Console.WriteLine("  scanotron document.pdf --force  # Regenerate pattern even if cached");
		Console.WriteLine("  scanotron document.pdf --json   # Machine-readable output");
		Console.WriteLine("  scanotron document.pdf --model gpt-4 --endpoint https://api.openai.com --apikey your-key");
		Console.WriteLine("  scanotron document.pdf -m qwen3:4b -e http://localhost:11434");
	}

	static CommandLineArguments ParseArguments(string[] args)
	{
		string? pdfFile = null;
		string? outputDirectory = null;
		string? model = null;
		string? endpoint = null;
		string? apiKey = null;
		var force = false;

		for (var argIndex = 0; argIndex < args.Length; argIndex++)
		{
			if (args[argIndex] == "--output" || args[argIndex] == "-o")
			{
				if (argIndex + 1 < args.Length)
				{
					outputDirectory = args[argIndex + 1];
					argIndex++; // Skip the next argument as it's the value for this parameter
				}
				else
				{
					Console.WriteLine($"Error: {args[argIndex]} requires a value.");
					return new CommandLineArguments(null, null, null, null, null, false);
				}
			}
			else if (args[argIndex] == "--model" || args[argIndex] == "-m")
			{
				if (argIndex + 1 < args.Length)
				{
					model = args[argIndex + 1];
					argIndex++; // Skip the next argument as it's the value for this parameter
				}
				else
				{
					Console.WriteLine($"Error: {args[argIndex]} requires a value.");
					return new CommandLineArguments(null, null, null, null, null, false);
				}
			}
			else if (args[argIndex] == "--endpoint" || args[argIndex] == "-e")
			{
				if (argIndex + 1 < args.Length)
				{
					endpoint = args[argIndex + 1];
					argIndex++; // Skip the next argument as it's the value for this parameter
				}
				else
				{
					Console.WriteLine($"Error: {args[argIndex]} requires a value.");
					return new CommandLineArguments(null, null, null, null, null, false);
				}
			}
			else if (args[argIndex] == "--apikey" || args[argIndex] == "-k")
			{
				if (argIndex + 1 < args.Length)
				{
					apiKey = args[argIndex + 1];
					argIndex++; // Skip the next argument as it's the value for this parameter
				}
				else
				{
					Console.WriteLine($"Error: {args[argIndex]} requires a value.");
					return new CommandLineArguments(null, null, null, null, null, false);
				}
			}
			else if (args[argIndex] == "--force")
			{
				force = true;
			}
			else if (args[argIndex] == "--json")
			{
				_jsonOutput = true;
			}
			else if (pdfFile == null && File.Exists(args[argIndex]))
			{
				// If no pdfFile parameter was specified yet and the argument is an existing file, treat it as the PDF file
				pdfFile = args[argIndex];
			}
			else if (pdfFile == null && !args[argIndex].StartsWith("-"))
			{
				// Treat the first non-option argument as the PDF file
				pdfFile = args[argIndex];
			}
			else if (args[argIndex].StartsWith("-"))
			{
				Console.WriteLine($"Error: Unknown argument '{args[argIndex]}'");
				ShowHelp();
				return new CommandLineArguments(null, null, null, null, null, false);
			}
		}

		return new CommandLineArguments(pdfFile, outputDirectory, model, endpoint, apiKey, force);
	}

	static async Task<string?> RunPdfbrrrAsync(string pdfFile, string? model, string? endpoint, string? apiKey)
	{
		try
		{
			var startInfo = new ProcessStartInfo
			{
				FileName = Path.Combine("..", "pdfbrrr", "bin", "Release", "net9.0", "pdfbrrr"),
				Arguments = $"\"{pdfFile}\" --prompt split-happens {(string.IsNullOrEmpty(model) ? "" : $" --model \"{model}\"")}{(string.IsNullOrEmpty(endpoint) ? "" : $" --endpoint \"{endpoint}\"")}{(string.IsNullOrEmpty(apiKey) ? "" : $" --apikey \"{apiKey}\"")}",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
				WorkingDirectory = Path.Combine("..", "pdfbrrr") // Set working directory to pdfbrrr directory
			};

			// Try alternative path for pdfbrrr if the first one doesn't exist
			if (!File.Exists(startInfo.FileName))
			{
				startInfo.FileName = Path.Combine("..", "pdfbrrr", "bin", "Debug", "net9.0", "pdfbrrr");
			}

			// Try .exe extension on Windows
			if (!File.Exists(startInfo.FileName) && Environment.OSVersion.Platform == PlatformID.Win32NT)
			{
				startInfo.FileName = Path.ChangeExtension(startInfo.FileName, ".exe");
			}

			LogInfo("Invoking pdfbrrr...", new
			{
				executable = startInfo.FileName,
				model = model ?? "default",
				endpoint = endpoint ?? "default"
			});

			using var process = new Process { StartInfo = startInfo };
			process.Start();

			var result = await process.StandardOutput.ReadToEndAsync();
			var error = await process.StandardError.ReadToEndAsync();

			await process.WaitForExitAsync();

			if (process.ExitCode != 0)
			{
				LogError($"pdfbrrr failed with exit code {process.ExitCode}", new { exitCode = process.ExitCode, error });
				return null;
			}

			// Show warnings/errors from stderr even if process succeeded
			if (!string.IsNullOrWhiteSpace(error))
			{
				LogWarning($"pdfbrrr warnings:\n{error.Trim()}");
			}

			if (!_jsonOutput)
			{
				LogInfo($"pdfbrrr raw output: '{result.Trim()}'");
			}

			return result.Trim();
		}
		catch (Exception ex)
		{
			LogError($"Error running pdfbrrr: {ex.Message}", new { exception = ex.GetType().Name });
			return null;
		}
	}

	static async Task<(bool success, int filesCreated, int totalPages)> RunSplitHappensAsync(string pdfFile, string pattern, string outputDirectory)
	{
		try
		{
			var startInfo = new ProcessStartInfo
			{
				FileName = Path.Combine("..", "split-happens", "bin", "Release", "net9.0", "split-happens"),
				Arguments = $"--file \"{pdfFile}\" --pattern \"{pattern}\" --output \"{outputDirectory}\"",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};

			// Try alternative path for split-happens if the first one doesn't exist


			if (!File.Exists(startInfo.FileName))
			{
				startInfo.FileName = Path.Combine("..", "split-happens", "bin", "Debug", "net9.0", "split-happens");
			}

			// Try .exe extension on Windows
			if (!File.Exists(startInfo.FileName) && Environment.OSVersion.Platform == PlatformID.Win32NT)
			{
				startInfo.FileName = Path.ChangeExtension(startInfo.FileName, ".exe");
			}

			LogInfo("Invoking split-happens...", new
			{
				executable = startInfo.FileName,
				pattern,
				outputDirectory
			});

			using var process = new Process { StartInfo = startInfo };
			process.Start();

			var output = await process.StandardOutput.ReadToEndAsync();
			var error = await process.StandardError.ReadToEndAsync();

			await process.WaitForExitAsync();

			// Parse output to extract information
			var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
			var filesCreated = 0;
			var totalPages = 0;
			var createdFiles = new List<string>();

			foreach (var line in lines)
			{
				if (line.Contains("Created:"))
				{
					filesCreated++;
					var fullPath = line.Split("Created:")[1].Split(" with")[0].Trim();
					var fileName = Path.GetFileName(fullPath);
					var groupInfo = line.Split(" with")[1].Trim();
					createdFiles.Add(fileName);

					if (!_jsonOutput)
					{
						LogSuccess($"Created: {fileName} with {groupInfo}");
					}
				}
				else if (!_jsonOutput && !string.IsNullOrWhiteSpace(line))
				{
					// Log other output lines only in non-JSON mode (no indentation)
					Console.WriteLine(line.Trim());
				}
			}

			if (_jsonOutput)
			{
				LogInfo("split-happens completed", new
				{
					filesCreated,
					totalPages,
					createdFiles
				});
			}

			if (!string.IsNullOrEmpty(error))
			{
				LogWarning($"split-happens warnings: {error}");
			}

			return (process.ExitCode == 0, filesCreated, totalPages);
		}
		catch (Exception ex)
		{
			LogError($"Error running split-happens: {ex.Message}", new { exception = ex.GetType().Name });
			return (false, 0, 0);
		}
	}

	// Logging Helper Methods
	private static void LogInfo(string message, object? data = null)
	{
		if (_jsonOutput)
		{
			var logEntry = new
			{
				timestamp = DateTime.UtcNow.ToString("o"),
				level = "INFO",
				message,
				data
			};
			var options = new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
			Console.WriteLine(JsonSerializer.Serialize(logEntry, options));
		}
		else
		{
			Console.WriteLine(message);
			if (data != null)
			{
				var options = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
				var jsonLines = JsonSerializer.Serialize(data, options).Split('\n');
				foreach (var line in jsonLines)
				{
					Console.WriteLine(line);
				}
			}
		}
	}

	private static void LogSuccess(string message, object? data = null)
	{
		if (_jsonOutput)
		{
			var logEntry = new
			{
				timestamp = DateTime.UtcNow.ToString("o"),
				level = "SUCCESS",
				message,
				data
			};
			var options = new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
			Console.WriteLine(JsonSerializer.Serialize(logEntry, options));
		}
		else
		{
			Console.WriteLine(message);
			if (data != null)
			{
				var options = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
				var jsonLines = JsonSerializer.Serialize(data, options).Split('\n');
				foreach (var line in jsonLines)
				{
					Console.WriteLine(line);
				}
			}
		}
	}

	private static void LogWarning(string message, object? data = null)
	{
		if (_jsonOutput)
		{
			var logEntry = new
			{
				timestamp = DateTime.UtcNow.ToString("o"),
				level = "WARNING",
				message,
				data
			};
			var options = new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
			Console.WriteLine(JsonSerializer.Serialize(logEntry, options));
		}
		else
		{
			Console.WriteLine($"⚠️  {message}");
			if (data != null)
			{
				var options = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
				var jsonLines = JsonSerializer.Serialize(data, options).Split('\n');
				foreach (var line in jsonLines)
				{
					Console.WriteLine(line);
				}
			}
		}
	}

	private static void LogError(string message, object? data = null)
	{
		if (_jsonOutput)
		{
			var logEntry = new
			{
				timestamp = DateTime.UtcNow.ToString("o"),
				level = "ERROR",
				message,
				data
			};
			var options = new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
			Console.WriteLine(JsonSerializer.Serialize(logEntry, options));
		}
		else
		{
			Console.WriteLine($"❌ {message}");
			if (data != null)
			{
				var options = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
				var jsonLines = JsonSerializer.Serialize(data, options).Split('\n');
				foreach (var line in jsonLines)
				{
					Console.WriteLine(line);
				}
			}
		}
	}

	private static void LogStep(int stepNumber, string title)
	{
		if (_jsonOutput)
		{
			var logEntry = new
			{
				timestamp = DateTime.UtcNow.ToString("o"),
				level = "STEP",
				step = stepNumber,
				title
			};
			var options = new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
			Console.WriteLine(JsonSerializer.Serialize(logEntry, options));
		}
		else
		{
			Console.WriteLine();
			Console.WriteLine("================================================================================");
			Console.WriteLine($"Step {stepNumber}: {title}");
			Console.WriteLine("================================================================================");
		}
	}

	private static void LogSummary(string title, Dictionary<string, object> data)
	{
		if (_jsonOutput)
		{
			var logEntry = new
			{
				timestamp = DateTime.UtcNow.ToString("o"),
				level = "SUMMARY",
				title,
				data
			};
			var options = new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
			Console.WriteLine(JsonSerializer.Serialize(logEntry, options));
		}
		else
		{
			Console.WriteLine();
			Console.WriteLine("================================================================================");
			Console.WriteLine(title);
			Console.WriteLine("================================================================================");
			foreach (var kvp in data)
			{
				Console.WriteLine($"{kvp.Key}: {kvp.Value}");
			}
			Console.WriteLine();
		}
	}
}