using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyComputer.CPU
{
    public readonly record struct AluResult(byte Value, bool Zero, bool Carry);

    public static class Alu
    {
        // Gate-level full adder (how real hardware does it)
        private static (bool Sum, bool Carry) FullAdder(bool a, bool b, bool carryIn)
        {
            bool x = a ^ b;                       // XOR gate
            bool sum = x ^ carryIn;               // XOR gate
            bool carryOut = (a & b) | (x & carryIn); // AND, AND, OR gates
            return (sum, carryOut);
        }

        /// <summary>8-bit ripple-carry adder built from full adders.</summary>
        public static AluResult AddGateLevel(byte a, byte b)
        {
            bool carry = false;
            byte result = 0;

            for (int bit = 0; bit < 8; bit++)
            {
                bool abit = ((a >> bit) & 1) == 1;
                bool bbit = ((b >> bit) & 1) == 1;

                (bool sum, carry) = FullAdder(abit, bbit, carry);

                if (sum) result |= (byte)(1 << bit);
            }

            return new AluResult(result, result == 0, carry);
        }

        // Fast versions used by the CPU
        public static AluResult Add(byte a, byte b)
        {
            int full = a + b;                     // may exceed 255
            byte result = (byte)(full & 0xFF);    // keep the lower 8 bits
            return new AluResult(result, result == 0, full > 0xFF);
        }

        public static AluResult Sub(byte a, byte b)
        {
            int full = a - b;                     // may go negative
            byte result = (byte)(full & 0xFF);    // wraps around (two's complement)
            return new AluResult(result, result == 0, a < b); // carry = borrow
        }
    }
}
