using Assembler.Tools;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Assembler
{
    internal class Program
    {
        public static Dictionary<string, string> computationTable = new Dictionary<string, string>
        {
            // a=0 computations
            { "0",   "0101010" },
            { "1",   "0111111" },
            { "-1",  "0111010" },
            { "D",   "0001100" },
            { "A",   "0110000" },
            { "!D",  "0001101" },
            { "!A",  "0110001" },
            { "-D",  "0001111" },
            { "-A",  "0110011" },
            { "D+1", "0011111" },
            { "A+1", "0110111" },
            { "D-1", "0001110" },
            { "A-1", "0110010" },
            { "D+A", "0000010" },
            { "D-A", "0010011" },
            { "A-D", "0000111" },
            { "D&A", "0000000" },
            { "D|A", "0010101" },

            // a=1 computations (for M operations)
            { "M",   "1110000" },
            { "!M",  "1110001" },
            { "-M",  "1110011" },
            { "M+1", "1110111" },
            { "M-1", "1110010" },
            { "D+M", "1000010" },
            { "D-M", "1010011" },
            { "M-D", "1000111" },
            { "D&M", "1000000" },
            { "D|M", "1010101" }
        };


        public static Dictionary<string, string> destinationTable = new Dictionary<string, string>
        {
            { "null", "000" }, // No destination.
            { "M",    "001" },
            { "D",    "010" },
            { "MD",   "011" },
            { "A",    "100" },
            { "AM",   "101" },
            { "AD",   "110" },
            { "AMD",  "111" }
        };

        public static Dictionary<string, string> jumpTable = new Dictionary<string, string>
        {
            { "null", "000" }, // No jump.
            { "JGT",  "001" },
            { "JEQ",  "010" },
            { "JGE",  "011" },
            { "JLT",  "100" },
            { "JNE",  "101" },
            { "JLE",  "110" },
            { "JMP",  "111" },
        };

        public static Dictionary<string, int> predefinedSymbols = new Dictionary<string, int>
        {
            { "SP", 0 },
            { "LCL", 1 },
            { "ARG", 2 },
            { "THIS", 3 },
            { "THAT", 4 },
            { "R0", 0 },
            { "R1", 1 },
            { "R2", 2 },
            { "R3", 3 },
            { "R4", 4 },
            { "R5", 5 },
            { "R6", 6 },
            { "R7", 7 },
            { "R8", 8 },
            { "R9", 9 },
            { "R10", 10 },
            { "R11", 11 },
            { "R12", 12 },
            { "R13", 13 },
            { "R14", 14 },
            { "R15", 15 },
            { "SCREEN", 16384 },
            { "KBD", 24576 }
        };

        static async Task Main(string[] args)
        {
            FileTool fileTool = new FileTool();
            TranslateToHackMachinecodes(fileTool.ProcessFile("C:\\Users\\Alexandar Lackovic\\source\\repos\\Assembler\\Data\\Pong.asm"), 
                "C:\\Users\\Alexandar Lackovic\\source\\repos\\Assembler\\Data\\Pong.hack"); //This is what file you write the hackcode to.
        }


        public static Dictionary<string, int> ResolveLabels(string cleanFile)
        {
            Dictionary<string, int> labelAddresses = new Dictionary<string, int>();
            int instructionCount = 0;

            foreach (var line in cleanFile.Split('\n'))
            {
                string trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("(") && trimmedLine.EndsWith(")"))
                {
                    string label = trimmedLine.Trim('(', ')').Trim();
                    labelAddresses[label] = instructionCount;
                }
                else
                {
                    instructionCount++;
                }
            }
            return labelAddresses;
        }

        public static string ProcessAInstruction(string line, Dictionary<string, int> labelAddresses, Dictionary<string, int> customVariableNamesAndValues)
        {
            string symbolOrValue = line.Substring(1).Trim();

            if (predefinedSymbols.TryGetValue(symbolOrValue, out int binaryValue))
            {
                return FilloutMissingBinaryNumbers(Convert.ToString(binaryValue, 2));
            }
            else if (labelAddresses.TryGetValue(symbolOrValue, out int labelAddress))
            {
                return FilloutMissingBinaryNumbers(Convert.ToString(labelAddress, 2));
            }
            else if (Int32.TryParse(symbolOrValue, out int number))
            {
                return FilloutMissingBinaryNumbers(Convert.ToString(number, 2));
            }
            else // Handle custom variable names.
            {
                if (customVariableNamesAndValues.TryGetValue(symbolOrValue, out int existingValue))
                {
                    return FilloutMissingBinaryNumbers(Convert.ToString(existingValue, 2));
                }
                else
                {
                    int nextAvailableAddress = (customVariableNamesAndValues.Count == 0)
                                               ? 16
                                               : customVariableNamesAndValues.Values.Max() + 1;

                    customVariableNamesAndValues.Add(symbolOrValue, nextAvailableAddress);
                    return FilloutMissingBinaryNumbers(Convert.ToString(nextAvailableAddress, 2));
                }
            }
        }


        public static string ProcessCInstruction(string line)
        {
            string cInstruction = "111"; // All C-instructions start with 111.
            string destKey = "null";
            string compKey;
            string jumpKey = "null";

            if (line.Contains(';'))
            {
                string[] jumpParts = line.Split(';');
                compKey = jumpParts[0].Trim();
                jumpKey = jumpParts[1].Trim();
            }
            else
            {
                compKey = line.Trim();
            }

            if (compKey.Contains('='))
            {
                string[] parts = compKey.Split('=');
                destKey = parts[0].Trim();
                compKey = parts[1].Trim();
            }

            if (computationTable.TryGetValue(compKey, out string computationValue))
            {
                cInstruction += computationValue;
            }
            if (destinationTable.TryGetValue(destKey, out string destinationValue))
            {
                cInstruction += destinationValue;
            }
            if (jumpTable.TryGetValue(jumpKey, out string jumpValue))
            {
                cInstruction += jumpValue;
            }
            return cInstruction;
        }


        public static void TranslateToHackMachinecodes(string cleanFile, string fileToWrite)
        {
            List<string> translatedLines = new List<string>();
            Dictionary<string, int> customVariableNamesAndValues = new Dictionary<string, int>();

            var labelAddresses = ResolveLabels(cleanFile);
            foreach (var line in cleanFile.Split('\n'))
            {
                string trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("(") && trimmedLine.EndsWith(")"))
                {
                    continue;
                }

                if (trimmedLine.StartsWith("@"))
                {
                    translatedLines.Add(ProcessAInstruction(trimmedLine, labelAddresses, customVariableNamesAndValues));
                }
                else
                {
                    translatedLines.Add(ProcessCInstruction(trimmedLine));
                }
            }
            File.WriteAllLines(fileToWrite, translatedLines);
        }

        public static string FilloutMissingBinaryNumbers(string shortBinary)
        {
            while (shortBinary.Length < 16)
            {
                shortBinary = "0" + shortBinary;
            }
            return shortBinary;
        }
    }
}