using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TinyComputer.CPU;

namespace TinyComputer.Utilities
{
    public static class Assembler
    {
        private static readonly Dictionary<string, (Opcode Op, bool HasOperand)> Table =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["NOP"] = (Opcode.Nop, false),
                ["LDA"] = (Opcode.Lda, true),
                ["LDI"] = (Opcode.Ldi, true),
                ["STA"] = (Opcode.Sta, true),
                ["ADD"] = (Opcode.Add, true),
                ["ADI"] = (Opcode.Adi, true),
                ["SUB"] = (Opcode.Sub, true),
                ["SBI"] = (Opcode.Sbi, true),
                ["JMP"] = (Opcode.Jmp, true),
                ["JZ"] = (Opcode.Jz, true),
                ["OUT"] = (Opcode.Out, false),
                ["HLT"] = (Opcode.Hlt, false),
            };

        public static byte[] Assemble(string source)
        {
            // Strip comments and blank lines
            var lines = source.Split('\n')
                .Select(l => { int i = l.IndexOf(';'); return (i >= 0 ? l[..i] : l).Trim(); })
                .Where(l => l.Length > 0);

            var labels = new Dictionary<string, int>();
            var instructions = new List<string[]>();
            int address = 0;

            // PASS 1: find the address of every label
            foreach (var line in lines)
            {
                string text = line;

                int colon = text.IndexOf(':');
                if (colon >= 0)
                {
                    labels[text[..colon].Trim()] = address;
                    text = text[(colon + 1)..].Trim();
                    if (text.Length == 0) continue;
                }

                var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (!Table.TryGetValue(parts[0], out var info))
                    throw new Exception($"Unknown instruction: {parts[0]}");

                address += info.HasOperand ? 2 : 1;
                instructions.Add(parts);
            }

            // PASS 2: emit bytes, resolving labels
            var bytes = new List<byte>();
            foreach (var parts in instructions)
            {
                var (op, hasOperand) = Table[parts[0]];
                bytes.Add((byte)op);

                if (hasOperand)
                {
                    if (parts.Length < 2)
                        throw new Exception($"{parts[0]} needs an operand");
                    bytes.Add(ParseOperand(parts[1], labels));
                }
            }

            return bytes.ToArray();
        }

        private static byte ParseOperand(string text, Dictionary<string, int> labels)
        {
            if (labels.TryGetValue(text, out int address)) return (byte)address;
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return Convert.ToByte(text[2..], 16);
            return byte.Parse(text);
        }
    }
}
