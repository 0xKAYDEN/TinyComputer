# TinyComputer

# Build a Tiny Computer in C#: How a CPU and Memory Work

This guide explains, from the ground up, how a CPU and memory work, and then walks you through building a small working computer in C# that can calculate `1 + 1`, `10 + 10`, and even run small loops.

---

## Table of Contents

1. [The Big Picture](#1-the-big-picture)
2. [Binary: the Only Language Hardware Speaks](#2-binary-the-only-language-hardware-speaks)
3. [How a Computer Adds: Logic Gates and the ALU](#3-how-a-computer-adds-logic-gates-and-the-alu)
4. [How Memory Works](#4-how-memory-works)
5. [How a CPU Works](#5-how-a-cpu-works)
6. [The Fetch-Decode-Execute Cycle](#6-the-fetch-decode-execute-cycle)
7. [Designing Our Tiny CPU (the Instruction Set)](#7-designing-our-tiny-cpu-the-instruction-set)
8. [Doing 1 + 1 By Hand](#8-doing-1--1-by-hand)
9. [Building It in C#](#9-building-it-in-c)
10. [Running 1 + 1 and 10 + 10](#10-running-1--1-and-10--10)
11. [A Mini Assembler](#11-a-mini-assembler)
12. [Loops and Conditions](#12-loops-and-conditions)
13. [Going Further](#13-going-further)
14. [Exercises](#14-exercises)
15. [Glossary](#15-glossary)

---

## 1. The Big Picture

A computer, at its simplest, is made of three things:

```
        +-----------+      address bus      +-----------+
        |           | --------------------> |           |
        |    CPU    |                       |  MEMORY   |
        |           | <==================>  |   (RAM)   |
        +-----------+      data bus         +-----------+
              |
              |  I/O (screen, keyboard, ...)
              v
        +-----------+
        |  OUTPUT   |
        +-----------+
```

- **Memory** stores numbers. Both your *program* (instructions) and your *data* live there as numbers.
- **CPU** repeatedly reads a number from memory, figures out what it means ("add", "store", "jump"), and does it.
- **Buses** are just the wires connecting them. In C#, a "bus" is simply a method call like `memory.Read(address)`.

That's it. Everything else (Windows, games, browsers) is built on top of this loop running billions of times per second.

The key insight: **a program is just numbers in memory.** The byte `0x02` might mean "load a value", and the byte after it is the value to load. The CPU doesn't know about "programs": it just reads bytes and obeys.

---

## 2. Binary: the Only Language Hardware Speaks

Hardware is made of switches that are either **off (0)** or **on (1)**. One such value is a **bit**. Eight bits make a **byte**.

| Binary     | Decimal | Hex  |
|------------|---------|------|
| `00000000` | 0       | 0x00 |
| `00000001` | 1       | 0x01 |
| `00000010` | 2       | 0x02 |
| `00001010` | 10      | 0x0A |
| `00010100` | 20      | 0x14 |
| `11111111` | 255     | 0xFF |

Each bit position is worth a power of two:

```
bit position:   7    6    5    4    3    2    1    0
value:        128   64   32   16    8    4    2    1

00001010 = 8 + 2 = 10
00010100 = 16 + 4 = 20
```

An 8-bit byte can hold values **0 to 255**. If you add `200 + 100 = 300`, the result doesn't fit; the hardware keeps the lower 8 bits (`300 - 256 = 44`) and sets a **carry flag** to say "something overflowed". We'll reproduce this behavior in C#.

**Hexadecimal** is just a compact way to write binary: each hex digit is exactly 4 bits. `0xFF = 1111 1111`.

---

## 3. How a Computer Adds: Logic Gates and the ALU

The part of the CPU that does math is the **ALU** (Arithmetic Logic Unit). It's built from **logic gates**.

### Basic gates

| Gate | Symbol in C# | Rule                          |
|------|--------------|-------------------------------|
| AND  | `a & b`      | 1 only if both inputs are 1   |
| OR   | `a \| b`     | 1 if at least one input is 1  |
| XOR  | `a ^ b`      | 1 if the inputs are different |
| NOT  | `!a`         | flips the bit                 |

### Adding two single bits (Half Adder)

Add two bits `A + B`:

| A | B | Sum (A XOR B) | Carry (A AND B) |
|---|---|---------------|-----------------|
| 0 | 0 | 0             | 0               |
| 0 | 1 | 1             | 0               |
| 1 | 0 | 1             | 0               |
| 1 | 1 | 0             | 1               |

Notice `1 + 1 = 10` in binary: the sum bit is `0` and the carry is `1`. That's literally how the computer computes `1 + 1`.

### Adding with a carry-in (Full Adder)

To chain bits together, each bit position also needs to accept the carry from the position before it:

```
Sum   = A XOR B XOR CarryIn
Carry = (A AND B) OR ((A XOR B) AND CarryIn)
```

### Ripple-Carry Adder (8 bits)

Chain 8 full adders; the carry-out of each feeds the carry-in of the next:

```
   bit0      bit1      bit2           bit7
  +-----+   +-----+   +-----+        +-----+
A0|     |A1 |     |A2 |     |  ...  A7|     |
B0| FA  |B1 | FA  |B2 | FA  |       B7| FA  |--> CarryOut
  +-----+   +-----+   +-----+        +-----+
   |  \_carry_/ |  \_carry_/              
  S0           S1          S2            S7
```

You will implement exactly this in C# in section 9.2. Subtraction works the same way using a trick called two's complement (invert the bits of B, add 1, then add), but our emulator will simply use C#'s `-` operator for that.

### What else does the ALU do?

Real ALUs also do AND, OR, XOR, shifts, and comparisons. After each operation they set **flags**:

- **Zero flag (Z)**: the result was 0.
- **Carry flag (C)**: the result didn't fit in 8 bits (or a borrow happened).
- (Real CPUs also have Negative and Overflow flags.)

Flags are how a CPU makes decisions: "jump if the last result was zero".

---

## 4. How Memory Works

Memory is a giant row of numbered boxes. Each box holds one byte.

```
Address   Value
-------   -----
0x00      0x02    <- instruction: LDI
0x01      0x01    <- its operand: the value 1
0x02      0x05    <- instruction: ADI
0x03      0x01    <- operand: 1
0x04      0x0A    <- instruction: OUT
0x05      0xFF    <- instruction: HLT
 ...       ...
0xFF      0x00
```

Memory only supports two operations:

| Operation | Inputs            | Output |
|-----------|-------------------|--------|
| **Read**  | an address        | the byte stored there |
| **Write** | an address + byte | nothing (the box now holds the new byte) |

Key facts:

- **Address**: the number of the box. With an 8-bit address you can reach 256 boxes (0-255). With 16 bits, 65,536 boxes (64 KB). A 64-bit CPU can address vastly more.
- **RAM is volatile**: its contents vanish without power. In C#, it's just a `byte[]`.
- **Programs and data share memory** (this is the *von Neumann architecture*). The CPU can't tell if a byte is code or data. It only treats it as code if the Program Counter points at it.
- **Multi-byte values**: to store numbers larger than 255 you use several bytes. **Little-endian** (x86, ARM) stores the least significant byte first: `0x1234` is stored as `34 12`. We'll stay with single bytes to keep things simple.

How real RAM works internally: each bit is a tiny capacitor (DRAM) or a small circuit of transistors (SRAM). An **address decoder** turns the address wires into "select exactly this one row". You don't need to emulate that: an array index does the same job.

---

## 5. How a CPU Works

The CPU is made of a few key parts:

```
+----------------------------------------------------------+
|                           CPU                            |
|                                                          |
|   +----------------+        +----------------------+     |
|   |   REGISTERS    |        |   CONTROL UNIT       |     |
|   |  A  (accum.)   |        |  decodes instruction |     |
|   |  PC (program   |        |  and orchestrates    |     |
|   |      counter)  |        |  everything          |     |
|   |  IR (instr.    |        +----------+-----------+     |
|   |      register) |                   |                 |
|   |  FLAGS (Z, C)  |                   v                 |
|   +-------+--------+        +----------------------+     |
|           |                 |         ALU          |     |
|           +---------------> |  add / sub / and ... |     |
|                             +----------------------+     |
+----------------------------------------------------------+
```

### Registers

Registers are tiny, ultra-fast storage slots *inside* the CPU. They hold the numbers the CPU is working on right now.

| Register | Name              | Purpose |
|----------|-------------------|---------|
| **A**    | Accumulator       | Holds the current working value; ALU results go here |
| **PC**   | Program Counter   | Address of the **next** instruction to fetch |
| **IR**   | Instruction Register | Holds the instruction currently being executed |
| **FLAGS**| Status flags      | Z (zero), C (carry), set by the ALU |

Why registers instead of working directly in memory? Speed. Registers are right next to the ALU; memory is far away.

### Control Unit

Reads the instruction in IR and decides what to do: "this is ADD, so tell the ALU to add and put the result in A". In real hardware it's a network of gates; in C# it's a `switch` statement.

### ALU

Does the math (section 3).

---

## 6. The Fetch-Decode-Execute Cycle

Every CPU in the world does this loop forever:

```
        +--------------------------------------+
        |                                      |
        v                                      |
   1. FETCH     IR = memory[PC]; PC = PC + 1   |
        |                                      |
        v                                      |
   2. DECODE    what does the number in IR mean?
        |                                      |
        v                                      |
   3. EXECUTE   do it (maybe read an operand,  |
        |       use the ALU, write memory...)  |
        +--------------------------------------+
```

Example, executing `LDI 1` (load immediate value 1 into A):

1. **Fetch**: read `memory[PC]` = `0x02` into IR. Increment PC.
2. **Decode**: `0x02` means "LDI: load the next byte into A".
3. **Execute**: read the next byte `memory[PC]` = `0x01`, put it in A. Increment PC again.

The Program Counter just counts upward through memory, so instructions run in sequence. **Jump** instructions overwrite PC with a new address, which is how loops and `if` statements exist at the lowest level.

The **clock** is what drives this: each tick advances the CPU one step. "3 GHz" means 3 billion ticks per second. In C#, one call to `cpu.Step()` is one iteration of the loop.

---

## 7. Designing Our Tiny CPU (the Instruction Set)

An **instruction set architecture (ISA)** is the list of things the CPU can do. We'll build an **8-bit accumulator machine** with **256 bytes of memory**.

- 8-bit registers, 8-bit addresses
- One working register: **A**
- Flags: **Zero**, **Carry**

| Opcode | Mnemonic    | Bytes | What it does |
|--------|-------------|-------|--------------|
| `0x00` | `NOP`       | 1 | Do nothing |
| `0x01` | `LDA addr`  | 2 | A = memory[addr] |
| `0x02` | `LDI value` | 2 | A = value (immediate) |
| `0x03` | `STA addr`  | 2 | memory[addr] = A |
| `0x04` | `ADD addr`  | 2 | A = A + memory[addr] |
| `0x05` | `ADI value` | 2 | A = A + value |
| `0x06` | `SUB addr`  | 2 | A = A - memory[addr] |
| `0x07` | `SBI value` | 2 | A = A - value |
| `0x08` | `JMP addr`  | 2 | PC = addr |
| `0x09` | `JZ addr`   | 2 | if Zero flag set: PC = addr |
| `0x0A` | `OUT`       | 1 | Output A (our "screen") |
| `0xFF` | `HLT`       | 1 | Stop the CPU |

"Immediate" means the value is stored right inside the instruction (the byte after the opcode). Otherwise the operand is an **address** pointing to a value in memory.

Only ALU operations (`ADD`, `ADI`, `SUB`, `SBI`) update the flags.

---

## 8. Doing 1 + 1 By Hand

Assembly program:

```
LDI 1     ; A = 1
ADI 1     ; A = A + 1
OUT       ; print A
HLT       ; stop
```

Machine code in memory:

| Address | Byte   | Meaning |
|---------|--------|---------|
| 0x00    | `0x02` | LDI |
| 0x01    | `0x01` | operand: 1 |
| 0x02    | `0x05` | ADI |
| 0x03    | `0x01` | operand: 1 |
| 0x04    | `0x0A` | OUT |
| 0x05    | `0xFF` | HLT |

Trace of execution:

| Step | PC before | Fetched | Action | A after | PC after |
|------|-----------|---------|--------|---------|----------|
| 1 | 0 | `LDI` | read operand 1, A = 1 | 1 | 2 |
| 2 | 2 | `ADI` | read operand 1, A = 1 + 1 | 2 | 4 |
| 3 | 4 | `OUT` | output A | 2 | 5 |
| 4 | 5 | `HLT` | halt | 2 | 6 |

Output: **2**. For `10 + 10`, change both operands to `10` (`0x0A`) and the result is **20**.

---

## 9. Building It in C#

Create a new console project:

```bash
dotnet new console -n TinyComputer
cd TinyComputer
```

We'll create these files: `Opcode.cs`, `Memory.cs`, `Alu.cs`, `Cpu.cs`, `Assembler.cs`, and `Program.cs`. (This code targets .NET 6 or newer.)

### 9.1 Opcodes

```csharp
// Opcode.cs
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
    Jz  = 0x09,   // if Zero: PC = addr
    Out = 0x0A,   // output A
    Hlt = 0xFF    // halt
}
```

### 9.2 The ALU

First, the **gate-level version** that shows how hardware really adds. Then the fast version the CPU will use.

```csharp
// Alu.cs
public readonly record struct AluResult(byte Value, bool Zero, bool Carry);

public static class Alu
{
    // ---------- Gate-level full adder (how real hardware does it) ----------
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

    // ---------- Fast versions used by the CPU ----------
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
```

Tip: if you want your CPU to be truly "from gates", call `AddGateLevel` inside `Add`. Both give identical results.

### 9.3 Memory

```csharp
// Memory.cs
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
```

### 9.4 The CPU

```csharp
// Cpu.cs
public sealed class Cpu
{
    private readonly Memory _memory;

    // Registers
    public byte A  { get; private set; }   // Accumulator
    public byte PC { get; private set; }   // Program Counter (a byte wraps at 256, like real hardware)
    public byte IR { get; private set; }   // Instruction Register

    // Flags
    public bool ZeroFlag  { get; private set; }
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
```

That's a complete CPU. Notice how closely the code mirrors the diagram: registers are fields, the control unit is the `switch`, and the ALU is a separate class.

---

## 10. Running 1 + 1 and 10 + 10

### Option 1: raw machine code (no assembler yet)

```csharp
// Program.cs
var memory = new Memory(256);
var cpu = new Cpu(memory);
cpu.OnOutput = value => Console.WriteLine($"OUTPUT: {value}");

// 1 + 1
memory.Load(new byte[]
{
    0x02, 0x01,   // LDI 1
    0x05, 0x01,   // ADI 1
    0x0A,         // OUT
    0xFF          // HLT
});
cpu.Run();
// OUTPUT: 2

// 10 + 10
cpu.Reset();
memory.Load(new byte[]
{
    0x02, 0x0A,   // LDI 10
    0x05, 0x0A,   // ADI 10
    0x0A,         // OUT
    0xFF          // HLT
});
cpu.Run();
// OUTPUT: 20
```

### Option 2: use memory for the data

This version keeps the two numbers in memory cells (like variables) and stores the result back:

```csharp
cpu.Reset();
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
```

Here you can see why memory matters: the numbers live there, the CPU pulls them into register A, computes, and writes the result back.

### Overflow demo

```csharp
cpu.Reset();
memory.Load(new byte[] { 0x02, 200, 0x05, 100, 0x0A, 0xFF }); // 200 + 100
cpu.Run();
cpu.PrintState();
// OUTPUT: 44, Carry flag = 1  (300 doesn't fit in a byte)
```

---

## 11. A Mini Assembler

Writing raw bytes gets old fast. An **assembler** turns readable text into machine code. This one supports comments (`;`), labels (`name:`), decimal and hex (`0x..`) numbers.

```csharp
// Assembler.cs
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
            ["JZ"]  = (Opcode.Jz,  true),
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
```

Now your programs are readable:

```csharp
string source = @"
    LDI 10      ; A = 10
    ADI 10      ; A = A + 10
    OUT         ; show it
    HLT
";

memory.Load(Assembler.Assemble(source));
cpu.Reset();
cpu.Run();   // OUTPUT: 20
```

---

## 12. Loops and Conditions

Jumps and the Zero flag are what turn a calculator into a computer. A countdown from 3:

```
        LDI 3        ; A = 3
loop:   OUT          ; print A
        SBI 1        ; A = A - 1   (sets Zero flag when A becomes 0)
        JZ  end      ; if A == 0, jump to end
        JMP loop     ; otherwise go back
end:    HLT
```

Output: `3`, `2`, `1`.

Multiplication by repeated addition (`5 x 4`), using memory cells as variables:

```
; mem[0x80] = counter (4), mem[0x81] = running total (0), add 5 each time
        LDI 4
        STA 0x80       ; counter = 4
        LDI 0
        STA 0x81       ; total = 0
loop:   LDA 0x81
        ADI 5
        STA 0x81       ; total += 5
        LDA 0x80
        SBI 1
        STA 0x80       ; counter -= 1
        JZ  done
        JMP loop
done:   LDA 0x81
        OUT            ; prints 20
        HLT
```

Notice how `if`, `while`, and `for` all become "compare, then jump". Every high-level language compiles down to this.

---

## 13. Going Further

Once the basics work, here is a natural roadmap, roughly in order of difficulty:

1. **More registers** (`B`, `C`, ...): add instructions like `MOV A, B`. Real CPUs have 8-32+ general-purpose registers.
2. **More ALU operations**: `AND`, `OR`, `XOR`, `NOT`, shift left/right.
3. **Compare instruction** (`CMP`): subtract without storing the result, just set flags. Add `JNZ`, `JC`, `JNC`.
4. **16-bit addresses and a bigger memory** (64 KB): change `PC` to `ushort`, and operands to two bytes (little-endian).
5. **The stack**: a region of memory plus a **Stack Pointer (SP)** register. Add `PUSH` and `POP`.
6. **Subroutines**: `CALL addr` pushes the return address and jumps; `RET` pops it back into PC. This is how functions work.
7. **Memory-mapped I/O**: reserve an address (say `0xFF`) so that writing to it prints a character on screen. Reading another address could return the last key pressed.
8. **Interrupts and a timer**: let hardware events pause the program and run a handler.
9. **A debugger**: single-step, breakpoints, register/memory viewer. `Step()` and `Dump()` already give you the foundation.
10. **A simple compiler**: write a tiny language (`x = 5 + 3;`) and compile it to your assembly.
11. **Emulate a real CPU**: once you understand this, the classic 6502 (NES, Apple II) or CHIP-8 are excellent next projects. CHIP-8 in particular is a famous "second emulator".

### How this differs from a real CPU

| Our tiny CPU | Real CPU |
|--------------|----------|
| Executes one instruction per `Step()` | Pipelines, executes several instructions at once, predicts branches |
| Memory access is instant | Multi-level caches (L1/L2/L3) hide RAM's slowness |
| 1 register + 2 flags | Dozens of registers, many flags, vector registers |
| 12 instructions | Hundreds to thousands |
| Runs in the "user's" world only | Privilege levels, virtual memory, MMU |

The *concept* is identical, though: fetch, decode, execute, repeat.

---

## 14. Exercises

1. **Subtraction**: write a program that computes `50 - 8` and prints it.
2. **Overflow**: add `250 + 10`. What does the CPU print? Which flags are set? Why?
3. **Memory swap**: store `7` in `mem[0x90]` and `9` in `mem[0x91]`, then swap them using only `LDA`/`STA`.
4. **Fibonacci**: print the first 10 Fibonacci numbers using a loop and memory cells.
5. **Add a new instruction**: implement `INC` (A = A + 1) and `DEC`. You only need a new enum value, a `case` in `Step()`, and an entry in the assembler table.
6. **Add `AND`/`OR`/`XOR`**: add them to the ALU. How would the Zero flag behave?
7. **Replace `Alu.Add` with `Alu.AddGateLevel`**: confirm all your programs still give the same results.
8. **Unit tests**: write xUnit tests for each instruction: set up memory, run, assert registers.
9. **Trace mode**: print `PC`, `IR`, `A`, flags after every `Step()` and compare with the hand trace in section 8.
10. **Bigger challenge**: widen the CPU to 16 bits so it can add `1000 + 2000`.

---

## 15. Glossary

| Term | Meaning |
|------|---------|
| **Bit** | A single 0 or 1 |
| **Byte** | 8 bits; values 0-255 |
| **Address** | The number identifying a memory cell |
| **Register** | Tiny fast storage inside the CPU |
| **Accumulator** | A register where arithmetic results accumulate |
| **PC (Program Counter)** | Register holding the address of the next instruction |
| **IR (Instruction Register)** | Register holding the current instruction |
| **ALU** | Arithmetic Logic Unit: the circuit that does math and logic |
| **Flag** | A single status bit (e.g. Zero, Carry) set by the ALU |
| **Opcode** | The number that identifies an instruction |
| **Operand** | The data/address an instruction works on |
| **Immediate** | An operand that is a literal value inside the instruction |
| **ISA** | Instruction Set Architecture: the list of instructions a CPU supports |
| **Machine code** | The raw bytes the CPU executes |
| **Assembly** | Human-readable text form of machine code |
| **Assembler** | Program that converts assembly to machine code |
| **Fetch-Decode-Execute** | The endless loop every CPU runs |
| **Bus** | The wires connecting CPU and memory (address bus, data bus) |
| **Clock** | The signal that drives each step of the CPU |
| **Carry** | A bit "overflowing" out of the top of an addition |
| **Von Neumann architecture** | Design where code and data share the same memory |
| **Endianness** | The byte order used for multi-byte numbers |

---

**Summary**: memory is an array of bytes; the CPU is a loop that reads a byte from memory at `PC`, decodes it with a `switch`, and executes it using registers and an ALU. Everything else (loops, functions, operating systems) is built by stacking ideas on top of that tiny cycle.