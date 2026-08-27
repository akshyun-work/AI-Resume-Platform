using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace FaceRecognitionAPI.Services
{
    public class FastApiHostedService : IHostedService
    {
        private Process? _pythonProcess;

        private readonly HttpClient _httpClient = new()
        {
            BaseAddress = new Uri("http://127.0.0.1:8000")
        };

        public async Task StartAsync(
            CancellationToken cancellationToken)
        {
            // ------------------------------------------------------------
            // FaceRecognitionService project directory
            // ------------------------------------------------------------

            var pythonProjectPath = Path.GetFullPath(
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "..",
                    "FaceRecognitionService"
                )
            );

            // ------------------------------------------------------------
            // Use the dedicated Face Recognition virtual environment
            // ------------------------------------------------------------

            var pythonPath = Path.Combine(
                pythonProjectPath,
                ".venv",
                "Scripts",
                "python.exe"
            );

            if (!File.Exists(pythonPath))
            {
                throw new FileNotFoundException(
                    "Face Recognition Python environment was not found.",
                    pythonPath);
            }

            // ------------------------------------------------------------
            // Start FastAPI
            // ------------------------------------------------------------

            var startInfo = new ProcessStartInfo
            {
                FileName = pythonPath,

                Arguments =
                    "-m uvicorn main:app --host 127.0.0.1 --port 8000",

                WorkingDirectory = pythonProjectPath,

                UseShellExecute = false,
                CreateNoWindow = true,

                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            _pythonProcess = Process.Start(startInfo);

            if (_pythonProcess is null)
            {
                throw new InvalidOperationException(
                    "Failed to start Face Recognition FastAPI process.");
            }

            // ------------------------------------------------------------
            // Wait until FastAPI is ready
            // ------------------------------------------------------------

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var response = await _httpClient.GetAsync(
                        "/index-status",
                        cancellationToken
                    );

                    if (response.IsSuccessStatusCode)
                    {
                        break;
                    }
                }
                catch (HttpRequestException)
                {
                    // FastAPI has not finished starting yet.
                }

                await Task.Delay(200, cancellationToken);
            }
        }

        public Task StopAsync(
            CancellationToken cancellationToken)
        {
            if (_pythonProcess is not null &&
                !_pythonProcess.HasExited)
            {
                _pythonProcess.Kill(
                    entireProcessTree: true);
            }

            return Task.CompletedTask;
        }
    }
}