using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace forward
{
    /// <summary>
    /// Represents a port forwarding rule.
    /// </summary>
    public class PortForwardingRule
    {
        public string SourceIp { get; set; } = "";
        public int SourcePort { get; set; }
        public string DestIp { get; set; } = "";
        public int DestPort { get; set; }
    }

    internal class Program
    {
        // File to store port forwarding rules.
        private static readonly string jsonFile = "forward.json";

        [RequiresUnreferencedCode("Calls forward.Program.SaveRules(List<PortForwardingRule>)")]
        [RequiresDynamicCode("Calls forward.Program.SaveRules(List<PortForwardingRule>)")]
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: forward [add|remove|apply|unapply] [parameters]");
                return;
            }

            string command = args[0].ToLower();
            switch (command)
            {
                case "add":
                    if (args.Length != 5)
                    {
                        Console.WriteLine("Usage: forward add <sourceIp> <sourcePort> <destIp> <destPort>");
                        return;
                    }
                    if (!int.TryParse(args[2], out int addSourcePort))
                    {
                        Console.WriteLine("Invalid source port.");
                        return;
                    }
                    if (!int.TryParse(args[4], out int addDestPort))
                    {
                        Console.WriteLine("Invalid destination port.");
                        return;
                    }

                    string addSourceIp = args[1];
                    string addDestIp = args[3];

                    var newRule = new PortForwardingRule
                    {
                        SourceIp = addSourceIp,
                        SourcePort = addSourcePort,
                        DestIp = addDestIp,
                        DestPort = addDestPort
                    };

                    var rules = LoadRules();
                    rules.Add(newRule);
                    SaveRules(rules);

                    AddPortForwarding(addSourceIp, addSourcePort, addDestIp, addDestPort);
                    Console.WriteLine("Rule added and applied.");
                    break;

                case "remove":
                    if (args.Length != 5)
                    {
                        Console.WriteLine("Usage: forward remove <sourceIp> <sourcePort> <destIp> <destPort>");
                        return;
                    }
                    if (!int.TryParse(args[2], out int remSourcePort))
                    {
                        Console.WriteLine("Invalid source port.");
                        return;
                    }
                    if (!int.TryParse(args[4], out int remDestPort))
                    {
                        Console.WriteLine("Invalid destination port.");
                        return;
                    }

                    string remSourceIp = args[1];
                    string remDestIp = args[3];

                    var ruleToRemove = new PortForwardingRule
                    {
                        SourceIp = remSourceIp,
                        SourcePort = remSourcePort,
                        DestIp = remDestIp,
                        DestPort = remDestPort
                    };

                    var currentRules = LoadRules();
                    // Remove matching rules based on all properties.
                    int removedCount = currentRules.RemoveAll(r =>
                        r.SourceIp == ruleToRemove.SourceIp &&
                        r.SourcePort == ruleToRemove.SourcePort &&
                        r.DestIp == ruleToRemove.DestIp &&
                        r.DestPort == ruleToRemove.DestPort);

                    if (removedCount > 0)
                    {
                        SaveRules(currentRules);
                        RemovePortForwarding(remSourceIp, remSourcePort, remDestIp, remDestPort);
                        Console.WriteLine("Rule removed and unapplied.");
                    }
                    else
                    {
                        Console.WriteLine("Rule not found.");
                    }
                    break;

                case "apply":
                    var applyRules = LoadRules();
                    foreach (var rule in applyRules)
                    {
                        AddPortForwarding(rule.SourceIp, rule.SourcePort, rule.DestIp, rule.DestPort);
                    }
                    Console.WriteLine("All rules applied.");
                    break;

                case "unapply":
                    var unapplyRules = LoadRules();
                    foreach (var rule in unapplyRules)
                    {
                        RemovePortForwarding(rule.SourceIp, rule.SourcePort, rule.DestIp, rule.DestPort);
                    }
                    Console.WriteLine("All rules unapplied.");
                    break;

                default:
                    Console.WriteLine("Unknown command. Usage: forward [add|remove|apply|unapply]");
                    break;
            }
        }

        /// <summary>
        /// Adds port forwarding rules via iptables.
        /// </summary>
        public static void AddPortForwarding(string sourceIp, int sourcePort, string destIp, int destPort)
        {
            Console.WriteLine($"Adding rule: {sourceIp}:{sourcePort} -> {destIp}:{destPort}");
            RunCommand("iptables", $"-t nat -A PREROUTING -p tcp -m tcp -d {sourceIp} --dport {sourcePort} -j DNAT --to-destination {destIp}:{destPort}");
            RunCommand("iptables", $"-A FORWARD -m state -p tcp -d {destIp} --dport {destPort} --state NEW,ESTABLISHED,RELATED -j ACCEPT");
            RunCommand("iptables", $"-t nat -A POSTROUTING -p tcp -m tcp -s {destIp} --sport {destPort} -j SNAT --to-source {sourceIp}");
            Console.WriteLine("Forwarding rule applied.");
        }

        /// <summary>
        /// Removes port forwarding rules via iptables.
        /// </summary>
        public static void RemovePortForwarding(string sourceIp, int sourcePort, string destIp, int destPort)
        {
            Console.WriteLine($"Removing rule: {sourceIp}:{sourcePort} -> {destIp}:{destPort}");
            RunCommand("iptables", $"-t nat -D PREROUTING -p tcp -m tcp -d {sourceIp} --dport {sourcePort} -j DNAT --to-destination {destIp}:{destPort}");
            RunCommand("iptables", $"-D FORWARD -m state -p tcp -d {destIp} --dport {destPort} --state NEW,ESTABLISHED,RELATED -j ACCEPT");
            RunCommand("iptables", $"-t nat -D POSTROUTING -p tcp -m tcp -s {destIp} --sport {destPort} -j SNAT --to-source {sourceIp}");
            Console.WriteLine("Forwarding rule removed.");
        }

        /// <summary>
        /// Executes a command with given arguments.
        /// </summary>
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

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                if (!string.IsNullOrEmpty(output))
                    Console.WriteLine(output);

                if (!string.IsNullOrEmpty(error))
                    Console.Error.WriteLine(error);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error executing {fileName} with arguments {arguments}: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads the list of port forwarding rules from a JSON file.
        /// </summary>
        [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
        [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
        private static List<PortForwardingRule> LoadRules()
        {
            if (!File.Exists(jsonFile))
            {
                return new List<PortForwardingRule>();
            }
            try
            {
                string json = File.ReadAllText(jsonFile);
                return JsonSerializer.Deserialize<List<PortForwardingRule>>(json) ?? new List<PortForwardingRule>();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error reading JSON file: " + ex.Message);
                return new List<PortForwardingRule>();
            }
        }

        /// <summary>
        /// Saves the list of port forwarding rules to a JSON file.
        /// </summary>
        [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
        [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
        private static void SaveRules(List<PortForwardingRule> rules)
        {
            try
            {
                string json = JsonSerializer.Serialize(rules, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(jsonFile, json);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error writing JSON file: " + ex.Message);
            }
        }
    }
}
