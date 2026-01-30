/*
* FILE          : Program.cs - (CLIENT)
* PROJECT       : A01 – TASKS 
* PROGRAMMER    : Najaf Ali, Che-Ping Chien, Precious Orewen, Yi-Chen Tsai
* FIRST VERSION : 2026-01-27
* DESCRIPTION   :
*      This program implements a TCP client that connects to a file monitoring server,
*      sends formatted messages containing client count, file size limit, and message content.
*      The client validates all input parameters, handles network communication, and displays
*      server responses with performance statistics.
*      
*      Key Features:
*      - Command-line argument parsing with validation
*      - Network communication with timeout handling
*      - Server response interpretation and display
*      - Performance timing and statistics
*      - Help system and usage instructions
*/

using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WDFA01Client
{
    class Program
    {
        /*
        * FUNCTION    : Main
        * DESCRIPTION : Entry point for the client application. Parses command-line arguments,
        *               validates input, establishes connection to server, sends message,
        *               and displays server response with performance statistics.
        * PARAMETERS  :
        *      string[] args: Command line arguments
        * RETURNS     : Task (async)
        */
        static async Task Main(string[] args)
        {
            Console.WriteLine("WDFA01 Client - TCP File Monitor");
            Console.WriteLine("==================================");

            // Display help if requested
            if (args.Length == 1 && (args[0] == "/?" || args[0] == "-?" || args[0] == "--help"))
            {
                DisplayUsage();
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
                return;
            }

            // Validate argument count
            if (args.Length != 3)
            {
                Console.WriteLine("ERROR: Incorrect number of arguments.");
                DisplayUsage();
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
                return;
            }

            // Parse arguments
            string clientNoStr = args[0];
            string fileSizeStr = args[1];
            string message = args[2];

            // Validate message content
            if (string.IsNullOrWhiteSpace(message))
            {
                Console.WriteLine("ERROR: Message cannot be empty or whitespace.");
                DisplayUsage();
                return;
            }

            if (message.Contains("|"))
            {
                Console.WriteLine("ERROR: Message cannot contain '|' character (reserved for protocol).");
                DisplayUsage();
                return;
            }

            // Validate file size parameter
            if (!int.TryParse(fileSizeStr, out int fileSize) || fileSize < 1 || fileSize > 10000000)
            {
                Console.WriteLine("ERROR: File size must be an integer between 1 and 10,000,000 bytes.");
                DisplayUsage();
                return;
            }

            // Validate client number parameter
            if (!int.TryParse(clientNoStr, out int clientNum) || clientNum < 1 || clientNum > 10000)
            {
                Console.WriteLine("ERROR: Client number must be an integer between 1 and 10,000.");
                DisplayUsage();
                return;
            }

            // Display configuration
            Console.WriteLine($"Client Configuration:");
            Console.WriteLine($"  Simulated Clients: {clientNum}");
            Console.WriteLine($"  File Size Limit: {fileSize} bytes");
            Console.WriteLine($"  Message: '{message}'");
            Console.WriteLine();

            // Format message based on content
            string clientMessage;
            if (message.Equals("Shutdown", StringComparison.OrdinalIgnoreCase))
            {
                clientMessage = "Shutdown";
                Console.WriteLine("Sending SHUTDOWN command to server...");
            }
            else
            {
                clientMessage = $"{clientNum}|{fileSize}|{message}";
            }

            // Start performance timer
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Load server configuration from App.config
                Int32 port = int.Parse(ConfigurationManager.AppSettings["Port"]);
                string serverAddr = ConfigurationManager.AppSettings["IP"];

                Console.WriteLine($"Connecting to server at {serverAddr}:{port}...");

                // Establish TCP connection
                using (TcpClient client = new TcpClient())
                {
                    client.SendTimeout = 5000;
                    client.ReceiveTimeout = 5000;

                    await client.ConnectAsync(IPAddress.Parse(serverAddr), port);
                    Console.WriteLine("✓ Connected to server successfully.");

                    using (NetworkStream stream = client.GetStream())
                    {
                        // Send message to server
                        byte[] data = Encoding.UTF8.GetBytes(clientMessage);
                        await stream.WriteAsync(data, 0, data.Length);
                        Console.WriteLine($"✓ Message sent to server.");

                        // Receive server response
                        byte[] buffer = new byte[1024];
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        stopwatch.Stop();

                        // Display server response
                        Console.WriteLine("\n===================================================");
                        Console.WriteLine($"SERVER RESPONSE: {response}");
                        Console.WriteLine("\n===================================================");

                        // Display client statistics
                        Console.WriteLine($"\nClient Statistics:");
                        Console.WriteLine($"  Execution Time: {stopwatch.ElapsedMilliseconds} ms");
                        Console.WriteLine($"  Execution Time: {stopwatch.Elapsed.TotalSeconds:F3} seconds");

                        // Interpret server response
                        if (response.StartsWith("STOP:"))
                        {
                            Console.WriteLine("\nSERVER NOTIFICATION: File size limit reached.");
                            Console.WriteLine("No more data should be sent to this server.");
                        }
                        else if (response.StartsWith("ERROR:"))
                        {
                            Console.WriteLine("\nSERVER ERROR: Request was not processed.");
                        }
                        else if (response.StartsWith("OK:"))
                        {
                            Console.WriteLine($"\nSUCCESS: {clientNum} simulated clients completed.");
                        }
                    }
                }
            }
            catch (SocketException sex)
            {
                stopwatch.Stop();
                Console.WriteLine($"\nNETWORK ERROR: {sex.Message}");
                Console.WriteLine($"Execution Time: {stopwatch.ElapsedMilliseconds} ms (failed)");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"\nERROR: {ex.Message}");
                Console.WriteLine($"Execution Time: {stopwatch.ElapsedMilliseconds} ms (failed)");
            }

            Console.WriteLine("\n===================================================");
            Console.WriteLine("Client execution complete.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        /*
        * FUNCTION    : DisplayUsage
        * DESCRIPTION : Displays usage instructions and program information.
        * RETURNS     : void
        */
        private static void DisplayUsage()
        {
            Console.WriteLine("USAGE:");
            Console.WriteLine("  WDFA01Client.exe <ClientNo> <FileSize> <Message>");
            Console.WriteLine();
            Console.WriteLine("ARGUMENTS:");
            Console.WriteLine("  ClientNo   : Number of simulated client threads (1-10000)");
            Console.WriteLine("  FileSize   : Maximum file size in bytes (1-10,000,000)");
            Console.WriteLine("  Message    : Message to send (cannot contain '|')");
            Console.WriteLine();
            Console.WriteLine("NOTES:");
            Console.WriteLine("  - Server IP and Port are configured in App.config file");
            Console.WriteLine("  - Use '/?', '-?', or '--help' to display this message");
            Console.WriteLine();
            Console.WriteLine("A01 – TASKS");
            Console.WriteLine("Modified by: Najaf Ali, Che-Ping Chien, Precious Orewen, Yi-Chen Tsai");
            Console.WriteLine("Conestoga College - SET Program");
        }
    }
}