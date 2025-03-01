using System.Diagnostics;

namespace forward
{
    internal class Program
    {
        static void Main(string[] args)
        {
        }

        /// <summary>
        /// Add port forwarding rules.
        /// </summary>
        /// <param name="sourceIp">External/source IP.</param>
        /// <param name="sourcePort">External/source port.</param>
        /// <param name="destIp">Internal/destination IP.</param>
        /// <param name="destPort">Internal/destination port.</param>
        public static void AddPortForwarding(string sourceIp, int sourcePort, string destIp, int destPort)
        {
            // Print out the provided parameters for review.
            Console.WriteLine($"Source IP (external): {sourceIp}");
            Console.WriteLine($"Source Port (external): {sourcePort}");
            Console.WriteLine($"Destination IP (internal): {destIp}");
            Console.WriteLine($"Destination Port (internal): {destPort}");
            // Add iptables rules.
            RunCommand("iptables", $"-t nat -A PREROUTING -p tcp -m tcp -d {sourceIp} --dport {sourcePort} -j DNAT --to-destination {destIp}:{destPort}");
            RunCommand("iptables", $"-A FORWARD -m state -p tcp -d {destIp} --dport {destPort} --state NEW,ESTABLISHED,RELATED -j ACCEPT");
            RunCommand("iptables", $"-t nat -A POSTROUTING -p tcp -m tcp -s {destIp} --sport {destPort} -j SNAT --to-source {sourceIp}");
            Console.WriteLine("Forwarding rule added.");
        }

        /// <summary>
        /// Remove port forwarding rules.
        /// </summary>
        /// <param name="sourceIp">External/source IP.</param>
        /// <param name="sourcePort">External/source port.</param>
        /// <param name="destIp">Internal/destination IP.</param>
        /// <param name="destPort">Internal/destination port.</param>
        public static void RemovePortForwarding(string sourceIp, int sourcePort, string destIp, int destPort)
        {
            // Print out the provided parameters for review.
            Console.WriteLine($"Source IP (external): {sourceIp}");
            Console.WriteLine($"Source Port (external): {sourcePort}");
            Console.WriteLine($"Destination IP (internal): {destIp}");
            Console.WriteLine($"Destination Port (internal): {destPort}");

            // Remove iptables rules.
            RunCommand("iptables", $"-t nat -D PREROUTING -p tcp -m tcp -d {sourceIp} --dport {sourcePort} -j DNAT --to-destination {destIp}:{destPort}");
            RunCommand("iptables", $"-D FORWARD -m state -p tcp -d {destIp} --dport {destPort} --state NEW,ESTABLISHED,RELATED -j ACCEPT");
            RunCommand("iptables", $"-t nat -D POSTROUTING -p tcp -m tcp -s {destIp} --sport {destPort} -j SNAT --to-source {sourceIp}");
            Console.WriteLine("Forwarding rule removed.");
        }


        /// <summary>
        /// Runs a command with the specified arguments.
        /// </summary>
        /// <param name="fileName">The command to execute (e.g., "iptables").</param>
        /// <param name="arguments">The command-line arguments.</param>
        private static void RunCommand(string fileName, string arguments)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process process = Process.Start(processInfo)!;
                process.WaitForExit();

                // Optionally, capture the output or errors.
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                if (!string.IsNullOrEmpty(output))
                {
                    Console.WriteLine(output);
                }
                if (!string.IsNullOrEmpty(error))
                {
                    Console.Error.WriteLine(error);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error executing {fileName} with arguments {arguments}: {ex.Message}");
            }
        }
    }
}
