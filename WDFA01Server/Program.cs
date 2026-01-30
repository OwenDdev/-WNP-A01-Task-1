/*
* FILE          : Program.cs (SERVER)
* PROJECT       : A01 – TASKS 
* PROGRAMMER    : Najaf Ali, Che-Ping Chien, Precious Orewen, Yi-Chen Tsai
* FIRST VERSION : 2026-01-27
* DESCRIPTION   :
*      This program implements a TCP server that accepts client connections, processes messages,
*      and writes data to a file until a specified size limit is reached. The server monitors
*      file size in real-time, provides performance statistics, and notifies clients when
*      the limit is reached. It supports graceful shutdown and concurrent client handling.
*      
*      Key Features:
*      - Concurrent client connection management
*      - File size monitoring with real-time reporting
*      - Thread-safe file operations using locks
*      - Client notification system for file limit reached
*      - Performance statistics tracking
*      - Graceful shutdown capability
*/

using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WDFA01Server
{
    class Program
    {
        // Thread synchronization objects
        private static readonly object fileLock = new object();        // Lock for file operations
        private static readonly object clientLock = new object();      // Lock for client list access

        // Control flags
        private static volatile bool stopWriting = false;              // Flag to stop all writing operations
        private static Stopwatch stopwatch = new Stopwatch();          // Timer for performance measurement

        // Client management
        private static List<TcpClient> connectedClients = new List<TcpClient>();  // List of active clients

        // Configuration and monitoring
        private static string filepath;                                // Path to output file
        private static int fileSizeLimit;                              // Maximum file size limit
        private static Task monitoringTask;                            // Background file monitoring task

        /*
        * FUNCTION    : Main
        * DESCRIPTION : Entry point for the server application. Initializes configuration,
        *               starts the TCP listener, and begins accepting client connections.
        * PARAMETERS  :
        *      string[] args: Command line arguments (not used)
        * RETURNS     : Task (async)
        */
        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("WDFA01 Server - TCP File Monitor");
                Console.WriteLine("==================================");

                // Load configuration from App.config
                Int32 port = int.Parse(ConfigurationManager.AppSettings["Port"]);
                string localAddr = ConfigurationManager.AppSettings["IP"];
                filepath = ConfigurationManager.AppSettings["File"];

                // Ensure output file exists
                EnsureFileExists(filepath);

                Console.WriteLine($"Configuration:");
                Console.WriteLine($"  IP Address: {localAddr}");
                Console.WriteLine($"  Port: {port}");
                Console.WriteLine($"  Output File: {filepath}");
                Console.WriteLine();

                // Start TCP listener
                TcpListener listener = new TcpListener(IPAddress.Parse(localAddr), port);
                listener.Start();
                Console.WriteLine($"Server started on {localAddr}:{port}");
                Console.WriteLine("Waiting for client connections...");
                Console.WriteLine("Press Ctrl+C to stop the server.");
                Console.WriteLine();

                // Start performance timer
                stopwatch.Start();

                // Begin accepting clients asynchronously
                var acceptTask = AcceptClientsAsync(listener);

                // Keep server running indefinitely
                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server initialization error: {ex.Message}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        /*
        * FUNCTION    : AcceptClientsAsync
        * DESCRIPTION : Continuously accepts incoming TCP client connections
        *               and spawns separate tasks to handle each client.
        * PARAMETERS  :
        *      TcpListener listener: TCP listener for accepting connections
        * RETURNS     : Task (async)
        */
        private static async Task AcceptClientsAsync(TcpListener listener)
        {
            while (!stopWriting)
            {
                try
                {
                    // Wait for client connection
                    TcpClient client = await listener.AcceptTcpClientAsync();

                    // Add client to connected list (thread-safe)
                    lock (clientLock)
                    {
                        connectedClients.Add(client);
                    }
                    Console.WriteLine($"Client connected. Total active clients: {connectedClients.Count}");

                    // Handle client in separate task
                    Task.Run(() => HandleClientAsync(client));
                }
                catch (Exception ex)
                {
                    if (!stopWriting)
                        Console.WriteLine($"Error accepting client: {ex.Message}");
                }
            }
        }

        /*
        * FUNCTION    : HandleClientAsync
        * DESCRIPTION : Handles communication with a connected client, processes messages,
        *               and manages file writing operations.
        * PARAMETERS  :
        *      TcpClient client: The connected client to handle
        * RETURNS     : Task (async)
        */
        private static async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                {
                    // Read message from client
                    byte[] buffer = new byte[1024];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                    if (bytesRead == 0)
                    {
                        Console.WriteLine("Client disconnected (no data received)");
                        return;
                    }

                    string fullMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Received: {fullMessage}");

                    // Check for shutdown command
                    if (fullMessage.Equals("Shutdown", StringComparison.OrdinalIgnoreCase))
                    {
                        await SendResponseAsync(stream, "Shutdown command received. Server will stop accepting new connections.");
                        await StopServerAsync();
                        return;
                    }

                    // Parse client message (format: ClientNo|FileSize|Message)
                    string[] messageParts = fullMessage.Split('|');

                    if (messageParts.Length != 3)
                    {
                        await SendResponseAsync(stream, "ERROR: Invalid input format. Expected: ClientNo|FileSize|Message");
                        return;
                    }

                    // Validate parsed values
                    if (!int.TryParse(messageParts[0], out int clientNo) ||
                        !int.TryParse(messageParts[1], out int fileSize) ||
                        clientNo < 1 || fileSize < 1)
                    {
                        await SendResponseAsync(stream, "ERROR: Client number and file size must be positive integers");
                        return;
                    }

                    string message = messageParts[2];

                    // Initialize file size monitoring if not already started
                    if (fileSizeLimit == 0)
                    {
                        fileSizeLimit = fileSize;
                        Console.WriteLine($"File size limit set to: {fileSizeLimit} bytes");
                        monitoringTask = Task.Run(() => MonitorFileSizeAsync());
                    }

                    // Write client messages to file
                    await WriteClientMessagesAsync(clientNo, message);

                    // Send appropriate response based on file size status
                    if (stopWriting)
                    {
                        await SendResponseAsync(stream, "STOP: File size limit reached. No more data accepted.");
                        await NotifyAllClientsToStopAsync();
                    }
                    else
                    {
                        await SendResponseAsync(stream, $"OK: {clientNo} messages written successfully");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error handling client: {e.Message}");
            }
            finally
            {
                // Remove client from connected list (thread-safe)
                lock (clientLock)
                {
                    connectedClients.Remove(client);
                }
                Console.WriteLine($"Client disconnected. Remaining active clients: {connectedClients.Count}");
            }
        }

        /*
        * FUNCTION    : WriteClientMessagesAsync
        * DESCRIPTION : Writes multiple messages to the output file concurrently,
        *               simulating multiple clients writing simultaneously.
        * PARAMETERS  :
        *      int numberOfClients: Number of simulated client messages to write
        *      string message: The message content to write
        * RETURNS     : Task (async)
        */
        private static async Task WriteClientMessagesAsync(int numberOfClients, string message)
        {
            Console.WriteLine($"Writing {numberOfClients} messages from simulated clients...");

            Task[] writeTasks = new Task[numberOfClients];

            // Create tasks for each simulated client
            for (int i = 0; i < numberOfClients; i++)
            {
                int clientId = i + 1;
                writeTasks[i] = Task.Run(() =>
                {
                    // Thread-safe file writing
                    lock (fileLock)
                    {
                        if (!stopWriting)
                        {
                            try
                            {
                                using (StreamWriter sw = new StreamWriter(filepath, true))
                                {
                                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                                    sw.WriteLine($"{timestamp} - [Client {clientId}] {message}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error writing to file: {ex.Message}");
                            }
                        }
                    }
                });
            }

            // Wait for all write operations to complete
            await Task.WhenAll(writeTasks);
            Console.WriteLine($"Completed writing {numberOfClients} messages to file.");
        }

        /*
        * FUNCTION    : MonitorFileSizeAsync
        * DESCRIPTION : Monitors the output file size in real-time, stops writing
        *               when the limit is reached, and displays performance statistics.
        * RETURNS     : Task (async)
        */
        private static async Task MonitorFileSizeAsync()
        {
            Console.WriteLine($"Starting file size monitoring. Target limit: {fileSizeLimit} bytes");

            while (!stopWriting)
            {
                try
                {
                    long currentSize = new FileInfo(filepath).Length;

                    // Display progress every 5 seconds
                    if (DateTime.Now.Second % 5 == 0)
                    {
                        Console.WriteLine($"Current file size: {currentSize} bytes ({100.0 * currentSize / fileSizeLimit:F1}% of limit)");
                    }

                    // Check if file size limit is reached
                    if (currentSize >= fileSizeLimit)
                    {
                        Console.WriteLine($"File size limit reached ({currentSize} >= {fileSizeLimit}). Stopping all writes...");
                        stopWriting = true;
                        stopwatch.Stop();

                        // Display performance statistics
                        Console.WriteLine($"\n--- PERFORMANCE STATISTICS ---");
                        Console.WriteLine($"Total execution time: {stopwatch.ElapsedMilliseconds} ms");
                        Console.WriteLine($"Total execution time: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
                        Console.WriteLine($"Final file size: {currentSize} bytes");
                        Console.WriteLine($"Average write rate: {currentSize / stopwatch.Elapsed.TotalSeconds:F2} bytes/second");
                        Console.WriteLine($"--------------------------------\n");

                        await NotifyAllClientsToStopAsync();
                        break;
                    }

                    await Task.Delay(1000); // Check every second
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error monitoring file size: {ex.Message}");
                }
            }
        }

        /*
        * FUNCTION    : NotifyAllClientsToStopAsync
        * DESCRIPTION : Sends stop notifications to all connected clients
        *               when file size limit is reached.
        * RETURNS     : Task (async)
        */
        private static async Task NotifyAllClientsToStopAsync()
        {
            Console.WriteLine("Notifying all connected clients to stop...");

            List<TcpClient> clientsToNotify;
            lock (clientLock)
            {
                clientsToNotify = new List<TcpClient>(connectedClients);
            }

            var notificationTasks = new List<Task>();

            // Send stop notification to each client
            foreach (var client in clientsToNotify)
            {
                notificationTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        if (client.Connected)
                        {
                            using (var stream = client.GetStream())
                            {
                                string stopMessage = "STOP: File size limit reached. Please stop sending data.";
                                byte[] stopBytes = Encoding.UTF8.GetBytes(stopMessage);
                                await stream.WriteAsync(stopBytes, 0, stopBytes.Length);
                                Console.WriteLine("Stop notification sent to client");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Silently handle notification errors (client may have disconnected)
                    }
                }));
            }

            await Task.WhenAll(notificationTasks);
            Console.WriteLine("All clients notified.");
        }

        /*
        * FUNCTION    : StopServerAsync
        * DESCRIPTION : Initiates graceful server shutdown, notifies clients,
        *               and closes all connections.
        * RETURNS     : Task (async)
        */
        private static async Task StopServerAsync()
        {
            stopWriting = true;
            stopwatch.Stop();

            Console.WriteLine("\nServer shutdown initiated.");
            Console.WriteLine($"Total server runtime: {stopwatch.Elapsed.TotalSeconds:F2} seconds");

            await NotifyAllClientsToStopAsync();

            // Close all client connections
            lock (clientLock)
            {
                Console.WriteLine($"Closing {connectedClients.Count} client connections...");
                foreach (var client in connectedClients)
                {
                    try { client.Close(); } catch { }
                }
                connectedClients.Clear();
            }

            Console.WriteLine("Server shutdown complete.");
            Console.WriteLine("Press any key to exit...");
            Environment.Exit(0);
        }

        /*
        * FUNCTION    : SendResponseAsync
        * DESCRIPTION : Sends a response message to a client over the network stream.
        * PARAMETERS  :
        *      NetworkStream stream: The network stream to write to
        *      string response: The response message to send
        * RETURNS     : Task (async)
        */
        private static async Task SendResponseAsync(NetworkStream stream, string response)
        {
            try
            {
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                Console.WriteLine($"Response sent: {response}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending response: {ex.Message}");
            }
        }

        /*
        * FUNCTION    : EnsureFileExists
        * DESCRIPTION : Ensures the output file and its directory exist,
        *               creating them if necessary.
        * PARAMETERS  :
        *      string filepath: Path to the output file
        * RETURNS     : void
        */
        private static void EnsureFileExists(string filepath)
        {
            try
            {
                string directory = Path.GetDirectoryName(filepath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Console.WriteLine($"Created directory: {directory}");
                }

                if (!File.Exists(filepath))
                {
                    File.Create(filepath).Close();
                    Console.WriteLine($"Created new output file: {filepath}");
                }
                else
                {
                    long currentSize = new FileInfo(filepath).Length;
                    Console.WriteLine($"Using existing file: {filepath}");
                    Console.WriteLine($"Current file size: {currentSize} bytes");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ensuring file exists: {ex.Message}");
                throw;
            }
        }
    }
}