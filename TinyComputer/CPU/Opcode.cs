using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyComputer.CPU
{
    public enum Opcode : byte
    {
        Nop = 0x00,
        Lda = 0x01,   // A = mem[addr]
        Ldi = 0x02,   // A = value
        Sta = 0x03,   // mem[addr] = A
        Add = 0x04,   // A += mem[addr]
        Adi = 0x05,   // A += value
        Sub = 0x06,   // A -= mem[addr]
        Sbi = 0x07,   // A -= value
        Jmp = 0x08,   // PC = addr
        Jz = 0x09,   // if Zero: PC = addr
        Out = 0x0A,   // output A
        Hlt = 0xFF    // halt
    }
}
