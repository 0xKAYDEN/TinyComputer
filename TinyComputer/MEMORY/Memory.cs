using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyComputer.MEMORY
{
    public sealed class Memory
    {
        private readonly byte[] _cells;

        public Memory(int size = 256) => _cells = new byte[size];

        public int Size => _cells.Length;

        public byte Read(int address)
        {
            CheckAddress(address);
            return _cells[address];
        }

        public void Write(int address, byte value)
        {
            CheckAddress(address);
            _cells[address] = value;
        }

        /// <summary>Copy a program into memory starting at an address.</summary>
        public void Load(byte[] program, int startAddress = 0)
        {
            if (startAddress + program.Length > _cells.Length)
                throw new ArgumentException("Program does not fit in memory.");

            Array.Copy(program, 0, _cells, startAddress, program.Length);
        }

        /// <summary>Print memory like a hex editor (great for debugging).</summary>
        public void Dump(int from = 0, int count = 64)
        {
            for (int i = from; i < from + count && i < _cells.Length; i += 16)
            {
                Console.Write($"{i:X2}: ");
                for (int j = i; j < i + 16 && j < from + count && j < _cells.Length; j++)
                    Console.Write($"{_cells[j]:X2} ");
                Console.WriteLine();
            }
        }

        private void CheckAddress(int address)
        {
            if (address < 0 || address >= _cells.Length)
                throw new IndexOutOfRangeException($"Bad memory address: 0x{address:X}");
        }
    }
}
