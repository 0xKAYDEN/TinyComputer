using TinyComputer.CPU;
using TinyComputer.Utilities;

namespace TinyComputer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var memory = new MEMORY.Memory(256);
            var cpu = new Cpu(memory);
            cpu.OnOutput = value => Console.WriteLine($"OUTPUT: {value}");

            //1+1=2
            memory.Load(new byte[]
{
    0x02, 0x01,   // LDI 1
    0x05, 0x01,   // ADI 1
    0x0A,         // OUT
    0xFF          // HLT
});
            cpu.Run();

            cpu.Reset();

            //10+10=20
            memory.Load(new byte[]
            {
    0x02, 0x0A,   // LDI 10
    0x05, 0x0A,   // ADI 10
    0x0A,         // OUT
    0xFF          // HLT
            });
            cpu.Run();


            cpu.Reset();

            //This version keeps the two numbers in memory cells (like variables) and stores the result back
            memory.Load(new byte[]
            {
    0x01, 0x80,   // LDA 0x80   ; A = mem[0x80]
    0x04, 0x81,   // ADD 0x81   ; A = A + mem[0x81]
    0x03, 0x82,   // STA 0x82   ; mem[0x82] = A
    0x0A,         // OUT
    0xFF          // HLT
            });
            memory.Write(0x80, 10);   // first number
            memory.Write(0x81, 10);   // second number
            cpu.Run();
            Console.WriteLine($"Result stored in memory[0x82] = {memory.Read(0x82)}");
            // OUTPUT: 20
            // Result stored in memory[0x82] = 20

            cpu.Reset();
            memory.Load(new byte[] { 0x02, 200, 0x05, 100, 0x0A, 0xFF }); // 200 + 100
            cpu.Run();
            cpu.PrintState();
            // OUTPUT: 44, Carry flag = 1  (300 doesn't fit in a byte)

            // Now let's use the assembler to write the same program in a more readable way
            string source = @"
            LDI 10      ; A = 10
            ADI 10      ; A = A + 10
            OUT         ; show it
            HLT
            ";

            memory.Load(Assembler.Assemble(source));
            cpu.Reset();
            cpu.Run();   // OUTPUT: 20

            Console.ReadLine();
        }
    }
}
