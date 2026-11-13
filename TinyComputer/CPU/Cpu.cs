using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TinyComputer.MEMORY;

namespace TinyComputer.CPU
{
    public sealed class Cpu
    {
        private readonly Memory _memory;

        // Registers
        public byte A { get; private set; }   // Accumulator
        public byte PC { get; private set; }   // Program Counter (a byte wraps at 256, like real hardware)
        public byte IR { get; private set; }   // Instruction Register

        // Flags
        public bool ZeroFlag { get; private set; }
        public bool CarryFlag { get; private set; }

        public bool Halted { get; private set; }
        public long Cycles { get; private set; }

        /// <summary>Called whenever the OUT instruction runs.</summary>
        public Action<byte>? OnOutput { get; set; }

        public Cpu(Memory memory) => _memory = memory;

        public void Reset()
        {
            A = PC = IR = 0;
            ZeroFlag = CarryFlag = false;
            Halted = false;
            Cycles = 0;
        }

        /// <summary>Read the byte at PC, then move PC forward.</summary>
        private byte FetchByte()
        {
            byte value = _memory.Read(PC);
            PC++;
            return value;
        }

        private void ApplyAlu(AluResult r)
        {
            A = r.Value;
            ZeroFlag = r.Zero;
            CarryFlag = r.Carry;
        }

        /// <summary>Execute exactly one instruction (one fetch-decode-execute cycle).</summary>
        public void Step()
        {
            if (Halted) return;

            // 1. FETCH
            IR = FetchByte();

            // 2. DECODE + 3. EXECUTE
            switch ((Opcode)IR)
            {
                case Opcode.Nop:
                    break;

                case Opcode.Lda:
                    A = _memory.Read(FetchByte());
                    break;

                case Opcode.Ldi:
                    A = FetchByte();
                    break;

                case Opcode.Sta:
                    _memory.Write(FetchByte(), A);
                    break;

                case Opcode.Add:
                    ApplyAlu(Alu.Add(A, _memory.Read(FetchByte())));
                    break;

                case Opcode.Adi:
                    ApplyAlu(Alu.Add(A, FetchByte()));
                    break;

                case Opcode.Sub:
                    ApplyAlu(Alu.Sub(A, _memory.Read(FetchByte())));
                    break;

                case Opcode.Sbi:
                    ApplyAlu(Alu.Sub(A, FetchByte()));
                    break;

                case Opcode.Jmp:
                    PC = FetchByte();
                    break;

                case Opcode.Jz:
                    {
                        byte target = FetchByte();   // always consume the operand
                        if (ZeroFlag) PC = target;
                        break;
                    }

                case Opcode.Out:
                    OnOutput?.Invoke(A);
                    break;

                case Opcode.Hlt:
                    Halted = true;
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unknown opcode 0x{IR:X2} at address 0x{(byte)(PC - 1):X2}");
            }

            Cycles++;
        }

        /// <summary>Run until HLT (with a safety limit to catch infinite loops).</summary>
        public void Run(long maxCycles = 1_000_000)
        {
            while (!Halted && Cycles < maxCycles)
                Step();

            if (!Halted)
                Console.WriteLine("[CPU] Stopped: cycle limit reached (infinite loop?)");
        }

        public void PrintState() =>
            Console.WriteLine(
                $"PC={PC:X2} IR={IR:X2} A={A:X2} ({A})  Z={(ZeroFlag ? 1 : 0)} C={(CarryFlag ? 1 : 0)}  cycles={Cycles}");
    }
}
