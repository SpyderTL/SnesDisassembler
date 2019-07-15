using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnesDisassembler
{
	class Program
	{
		static byte[] Data;
		static int Current;
		static string Text = string.Empty;
		static int Next = -1;
		static int Branch = -1;
		static string Line = string.Empty;
		static Stack<int> Stack = new Stack<int>();
		static byte Flags;
		static string Instruction = string.Empty;
		static string Address = string.Empty;

		static void Main(string[] args)
		{
			var fileName = @"..\..\Examples\StarFoxUsa10.bin";

			ReadFile(fileName);
			Current = BitConverter.ToUInt16(Data, 0xfffc);

			Disassemble();

			Console.WriteLine(Text);

			while (true)
			{
				Console.Write(":");
				Line = Console.ReadLine();

				Execute();

				Disassemble();

				Console.WriteLine(Text);
			}
		}

		private static void Execute()
		{
			if (string.IsNullOrWhiteSpace(Line))
				Current = Next;
			else
			{
				var fields = Line.ToLower().Split(' ');

				switch (fields[0])
				{
					case "a":
						Flags ^= 0x20;
						break;

					case "b":
						Current = Branch;
						break;

					case "c":
						Stack.Push(Next);
						Current = Branch;
						break;

					case "s":
						Scan();
						break;

					case "t":
						var address = Current;
						var columns = 16;
						var rows = 16;

						if (fields.Length > 1)
							int.TryParse(fields[1], System.Globalization.NumberStyles.HexNumber, null, out address);

						if (fields.Length > 2)
							int.TryParse(fields[2], out columns);

						if (fields.Length > 3)
							int.TryParse(fields[3], out rows);

						Table(address, columns, rows);
						break;

					case "x":
						Flags ^= 0x10;
						break;

					default:
						int.TryParse(fields[0], System.Globalization.NumberStyles.HexNumber, null, out address);
						Current = address;
						break;
				}
			}
		}

		private static void Table(int address, int columns, int rows)
		{
			for (var row = 0; row < rows; row++)
			{
				for (var column = 0; column < columns; column++)
				{
					if (column != 0)
						Console.Write(" ");

					Console.Write(Data[address + (row * columns) + column].ToString("X2"));
				}

				Console.WriteLine();
			}
		}

		private static void Scan()
		{
			var text = Text;
			var current = Current;
			var flags = Flags;
			var next = Next;
			var branch = Branch;
			var stack = Stack;

			var jumps = new List<int>();
			var calls = new List<int>();
			var branches = new List<int>();
			var reads = new List<string>();
			var writes = new List<string>();

			var newBranches = new Stack<int>();
			var oldBranches = new List<int>();

			var done = false;

			while (!done)
			{
				Current = Next;

				Disassemble();

				switch (Instruction)
				{
					case "Jump":
						jumps.Add(Next);

						if (oldBranches.Contains(Next) ||
							newBranches.Contains(Next))
						{
							if (newBranches.Count == 0)
								done = true;
							else
							{
								Next = newBranches.Pop();
								oldBranches.Add(Next);
							}
						}
						else
							newBranches.Push(Next);
						break;

					case "Call":
						calls.Add(Branch);
						break;

					case "Branch":
						if (oldBranches.Contains(Branch) ||
							newBranches.Contains(Branch))
							newBranches.Push(Branch);

						break;

					case "Return":
						if (newBranches.Count == 0)
							done = true;
						else
						{
							Next = newBranches.Pop();
							oldBranches.Add(Next);
						}
						break;

					case "Read":
						reads.Add(Address);
						break;

					case "Write":
						writes.Add(Address);
						break;
				}
			}

			Console.WriteLine("Calls: \r\n" + string.Join(Environment.NewLine, calls.Distinct().OrderBy(x => x).Select(x => x.ToString("X6"))));
			Console.WriteLine();
			Console.WriteLine("Reads: \r\n" + string.Join(Environment.NewLine, reads.Distinct().OrderBy(x => x)));
			Console.WriteLine();
			Console.WriteLine("Writes: \r\n" + string.Join(Environment.NewLine, writes.Distinct().OrderBy(x => x)));
			Console.WriteLine();

			Text = text;
			Current = current;
			Flags = flags;
			Next = next;
			Branch = branch;
			Stack = stack;
		}

		private static void Disassemble()
		{
			var instruction = Data[Current];

			Instruction = string.Empty;
			Address = string.Empty;

			switch (instruction)
			{
				case 0x00:
					Text = Current.ToString("X6") + " Break";
					Next = Current + 1;
					break;

				//case 0x02:


				case 0x03:
					var address = (int)Data[Current + 1];
					Text = Current.ToString("X6") + " OrAccumulatorWithStackRelativeAddress " + address.ToString("X2");
					Instruction = "Read";
					Address = "S+" + address.ToString("X2");
					Next = Current + 2;
					break;

				case 0x06:
					address = (int)Data[Current + 1];
					Text = Current.ToString("X6") + " ShiftDirectAddressLeft " + address.ToString("X2");
					Instruction = "Write";
					Address = address.ToString("X2");
					Next = Current + 2;
					break;

				case 0x08:
					Text = Current.ToString("X6") + " PushFlags";
					Next = Current + 1;
					Stack.Push(Flags);
					break;

				case 0x09:
					int value;
					if ((Flags & 0x20) == 0)
					{
						value = Data[Current + 1] | Data[Current + 2] << 8;
						Text = Current.ToString("X6") + " OrAccumulatorWithImmediate " + value.ToString("X4");
						Next = Current + 3;
					}
					else
					{
						value = Data[Current + 1];
						Text = Current.ToString("X6") + " OrAccumulatorWithImmediate " + value.ToString("X2");
						Next = Current + 2;
					}
					break;

				case 0x0a:
					Text = Current.ToString("X6") + " ShiftAccumulatorLeft";
					Next = Current + 1;
					break;

				case 0x10:
					address = Current + 2 + (sbyte)Data[Current + 1];
					Text = Current.ToString("X6") + " BranchToRelativeIfPositive " + address.ToString("X6");
					Instruction = "Branch";
					Next = Current + 2;
					Branch = address;
					break;

				case 0x15:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " OrAccumulatorWithDirectAddressPlusXIndex " + address.ToString("X2");
					Instruction = "Read";
					Address = address.ToString("X2") + "+X";
					Next = Current + 2;
					break;

				case 0x16:
					address = (int)Data[Current + 1];
					Text = Current.ToString("X6") + " ShiftDirectAddressPlusXIndexLeft " + address.ToString("X2");
					Instruction = "Write";
					Address = address.ToString("X2");
					Next = Current + 2;
					break;

				case 0x18:
					Text = Current.ToString("X6") + " ClearCarryFlag";
					Next = Current + 1;
					break;

				case 0x1a:
					Text = Current.ToString("X6") + " IncrementAccumulator";
					Next = Current + 1;
					break;

				case 0x20:
					address = Data[Current + 1] | (Data[Current + 2] << 8) | (Current & 0xff0000);
					Text = Current.ToString("X6") + " CallAbsoluteAddress " + address.ToString("X6");
					Instruction = "Call";
					Address = address.ToString("X6");
					Next = Current + 3;
					Branch = address;
					break;

				case 0x21:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " AndAccumulatorWithDirectAddressPlusXIndexPointer " + address.ToString("X2");
					Instruction = "Read";
					Address = "[" + address.ToString("X2") + "+X]";
					Next = Current + 2;
					break;

				case 0x22:
					address = Data[Current + 1] | (Data[Current + 2] << 8) | (Data[Current + 3] << 16);
					Text = Current.ToString("X6") + " CallAbsoluteLongAddress " + address.ToString("X6");
					Instruction = "Call";
					Address = address.ToString("X6");
					Next = Current + 4;
					Branch = address;
					break;

				case 0x28:
					Text = Current.ToString("X6") + " PullFlags";
					Next = Current + 1;
					Flags = (byte)Stack.Pop();
					break;

				case 0x29:
					if ((Flags & 0x20) == 0)
					{
						value = Data[Current + 1] | Data[Current + 2] << 8;
						Text = Current.ToString("X6") + " AndAccumulatorWithImmediate " + value.ToString("X4");
						Next = Current + 3;
					}
					else
					{
						value = Data[Current + 1];
						Text = Current.ToString("X6") + " AndAccumulatorWithImmediate " + value.ToString("X2");
						Next = Current + 2;
					}
					break;

				case 0x2a:
					Text = Current.ToString("X6") + " RotateAccumulatorLeft";
					Next = Current + 1;
					break;

				case 0x30:
					address = Current + 2 + (sbyte)Data[Current + 1];
					Text = Current.ToString("X6") + " BranchToRelativeIfNegative " + address.ToString("X6");
					Instruction = "Branch";
					Address = address.ToString("X6");
					Next = Current + 2;
					Branch = address;
					break;

				case 0x32:
					address = (int)Data[Current + 1];
					Text = Current.ToString("X6") + " AndAccumulatorWithDirectAddressPointer " + address.ToString("X2");
					Instruction = "Read";
					Address = "[" + address.ToString("X2") + "]";
					Next = Current + 2;
					break;

				case 0x38:
					Text = Current.ToString("X6") + " SetCarryFlag";
					Next = Current + 1;
					break;

				case 0x39:
					address = Data[Current + 1] | (Data[Current + 2] << 8);
					Text = Current.ToString("X6") + " AndAccumulatorWithAbsoluteAddressPlusYIndex " + address.ToString("X4");
					Instruction = "Read";
					Address = address.ToString("X4") + "+Y";
					Next = Current + 3;
					break;

				case 0x3a:
					Text = Current.ToString("X6") + " DecrementAccumulator";
					Next = Current + 1;
					break;

				case 0x3d:
					address = Data[Current + 1] | (Data[Current + 2] << 8);
					Text = Current.ToString("X6") + " AndAccumulatorWithAbsoluteAddressPlusXIndex " + address.ToString("X4");
					Instruction = "Read";
					Address = address.ToString("X4") + "+X";
					Next = Current + 3;
					break;

				case 0x40:
					Text = Current.ToString("X6") + " ReturnFromInterrupt";
					Instruction = "Return";
					break;

				case 0x46:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " ShiftDirectAddressRight " + address.ToString("X2");
					Instruction = "Write";
					Address = address.ToString("X2");
					Next = Current + 2;
					break;

				case 0x48:
					Text = Current.ToString("X6") + " PushAccumulator";
					Next = Current + 1;
					break;

				case 0x49:
					if ((Flags & 0x20) == 0)
					{
						value = Data[Current + 1] | Data[Current + 2] << 8;
						Text = Current.ToString("X6") + " ExclusiveOrAccumulatorWithImmediate " + value.ToString("X4");
						Next = Current + 3;
					}
					else
					{
						value = Data[Current + 1];
						Text = Current.ToString("X6") + " ExclusiveOrAccumulatorWithImmediate " + value.ToString("X2");
						Next = Current + 2;
					}
					break;

				case 0x4a:
					Text = Current.ToString("X6") + " ShiftAccumulatorRight";
					Next = Current + 1;
					break;

				case 0x4b:
					Text = Current.ToString("X6") + " PushProgramBank";
					Next = Current + 1;
					break;

				case 0x4c:
					address = Data[Current + 1] | (Data[Current + 2] << 8) | Current & 0xff0000;
					Text = Current.ToString("X6") + " JumpToAbsoluteAddress " + address.ToString("X6");
					Instruction = "Jump";
					Address = address.ToString("X6");
					Next = address;
					break;

				case 0x58:
					Text = Current.ToString("X6") + " ClearInterruptDisableFlag";
					Next = Current + 1;
					break;

				case 0x5a:
					Text = Current.ToString("X6") + " PushYIndex";
					Next = Current + 1;
					break;

				case 0x5b:
					Text = Current.ToString("X6") + " CopyAccumulatorToDirectPageRegister";
					Next = Current + 1;
					break;

				case 0x5c:
					address = Data[Current + 1] | (Data[Current + 2] << 8) | Data[Current + 3] << 16;
					Text = Current.ToString("X6") + " JumpToAbsoluteLongAddress " + address.ToString("X6");
					Instruction = "Jump";
					Address = address.ToString("X6");
					Next = address;
					break;

				case 0x60:
					Text = Current.ToString("X6") + " ReturnToCaller";
					Instruction = "Return";
					Next = Stack.Count != 0 ? Stack.Pop() : Current;
					break;

				case 0x63:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " AddStackRelativeAddressToAccumulator " + address.ToString("X2");
					Instruction = "Read";
					Address = "S+" + address.ToString("X6");
					Next = Current + 2;
					break;

				case 0x64:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " SetDirectAddressToZero " + address.ToString("X2");
					Instruction = "Write";
					Address = address.ToString("X2");
					Next = Current + 2;
					break;

				case 0x65:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " AddDirectAddressToAccumulator " + address.ToString("X2");
					Instruction = "Read";
					Address = address.ToString("X2");
					Next = Current + 2;
					break;

				case 0x67:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " AddDirectAddressLongPointerToAccumulator " + address.ToString("X2");
					Instruction = "Read";
					Address = "[" + address.ToString("X2") + "]";
					Next = Current + 2;
					break;

				case 0x68:
					Text = Current.ToString("X6") + " PullAccumulator";
					Next = Current + 1;
					break;

				case 0x69:
					if ((Flags & 0x20) == 0)
					{
						value = Data[Current + 1] | Data[Current + 2] << 8;
						Text = Current.ToString("X6") + " AddImmediateToAccumulator " + value.ToString("X4");
						Next = Current + 3;
					}
					else
					{
						value = Data[Current + 1];
						Text = Current.ToString("X6") + " AddImmediateToAccumulator " + value.ToString("X2");
						Next = Current + 2;
					}
					break;

				case 0x6a:
					Text = Current.ToString("X6") + " RotateAccumulatorRight";
					Next = Current + 1;
					break;

				case 0x6b:
					Text = Current.ToString("X6") + " ReturnToLongCaller";
					Instruction = "Return";
					Next = Stack.Count != 0 ? Stack.Pop() : Current;
					break;

				case 0x6d:
					address = Data[Current + 1] | (Data[Current + 2] << 8);
					Text = Current.ToString("X6") + " AddAbsoluteAddressToAccumulator " + address.ToString("X4");
					Instruction = "Read";
					Address = address.ToString("X4");
					Next = Current + 3;
					break;

				case 0x74:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " SetDirectAddressPlusXIndexToZero " + address.ToString("X2");
					Instruction = "Write";
					Address = address.ToString("X2") + "+X";
					Next = Current + 2;
					break;

				case 0x75:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " AddDirectAddressPlusXIndexToAccumulator " + address.ToString("X2");
					Instruction = "Read";
					Address = address.ToString("X2") + "+X";
					Next = Current + 2;
					break;

				case 0x78:
					Text = Current.ToString("X6") + " SetInterruptDisableFlag";
					Next = Current + 1;
					break;

				case 0x7a:
					Text = Current.ToString("X6") + " PullYIndex";
					Next = Current + 1;
					break;

				case 0x7f:
					address = Data[Current + 1] | Data[Current + 2] << 8 | Data[Current + 3] << 16;
					Text = Current.ToString("X6") + " AddAbsoluteLongAddressPlusXIndexToAccumulator " + address.ToString("X6");
					Instruction = "Read";
					Address = address.ToString("X6") + "+X";
					Next = Current + 4;
					break;

				case 0x80:
					address = Current + 2 + (sbyte)Data[Current + 1];
					Text = Current.ToString("X6") + " JumpToRelative " + address.ToString("X6");
					Instruction = "Jump";
					Address = address.ToString("X6");
					Next = address;
					break;

				case 0x82:
					address = Current + 3 + BitConverter.ToInt16(Data, Current + 1);
					Text = Current.ToString("X6") + " JumpToRelativeLong " + address.ToString("X6");
					Instruction = "Jump";
					Address = address.ToString("X6");
					Next = address;
					break;

				case 0x85:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " CopyAccumulatorToDirectAddress " + address.ToString("X2");
					Instruction = "Write";
					Address = address.ToString("X2");
					Next = Current + 2;
					break;

				case 0x86:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " CopyXIndexToDirectAddress " + address.ToString("X2");
					Instruction = "Write";
					Address = address.ToString("X2");
					Next = Current + 2;
					break;

				case 0x88:
					Text = Current.ToString("X6") + " DecrementYIndex";
					Next = Current + 1;
					break;

				case 0x89:
					if ((Flags & 0x20) == 0)
					{
						value = Data[Current + 1] | Data[Current + 2] << 8;
						Text = Current.ToString("X6") + " TestImmediate " + value.ToString("X4");
						Next = Current + 3;
					}
					else
					{
						value = Data[Current + 1];
						Text = Current.ToString("X6") + " TestImmediate " + value.ToString("X2");
						Next = Current + 2;
					}
					break;

				case 0x8a:
					Text = Current.ToString("X6") + " CopyXIndexToAccumulator";
					Next = Current + 1;
					break;

				case 0x8b:
					Text = Current.ToString("X6") + " PushDataBank";
					Next = Current + 1;
					break;

				case 0x8c:
					address = Data[Current + 1] | (Data[Current + 2] << 8);
					Text = Current.ToString("X6") + " CopyYIndexToAbsoluteAddress " + address.ToString("X4");
					Instruction = "Write";
					Address = address.ToString("X4");
					Next = Current + 3;
					break;

				case 0x8d:
					address = Data[Current + 1] | (Data[Current + 2] << 8);
					Text = Current.ToString("X6") + " CopyAccumulatorToAbsoluteAddress " + address.ToString("X4");
					Instruction = "Write";
					Address = address.ToString("X4");
					Next = Current + 3;
					break;

				case 0x8e:
					address = Data[Current + 1] | (Data[Current + 2] << 8);
					Text = Current.ToString("X6") + " CopyXIndexToAbsoluteAddress " + address.ToString("X4");
					Instruction = "Write";
					Address = address.ToString("X4");
					Next = Current + 3;
					break;

				case 0x8f:
					address = Data[Current + 1] | Data[Current + 2] << 8 | Data[Current + 3] << 16;
					Text = Current.ToString("X6") + " CopyAccumulatorToAbsoluteLongAddress " + address.ToString("X6");
					Instruction = "Write";
					Address = address.ToString("X6");
					Next = Current + 4;
					break;

				case 0x90:
					address = Current + 2 + (sbyte)Data[Current + 1];
					Text = Current.ToString("X6") + " BranchToRelativeIfLessThan " + address.ToString("X6");
					Instruction = "Branch";
					Next = Current + 2;
					Branch = address;
					break;

				case 0x95:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " CopyAccumulatorToDirectAddressPlusXIndex " + address.ToString("X2");
					Instruction = "Write";
					Address = address.ToString("X2");
					Next = Current + 2;
					break;

				case 0x98:
					Text = Current.ToString("X6") + " CopyYIndexToAccumulator";
					Next = Current + 1;
					break;

				case 0x99:
					address = Data[Current + 1] | (Data[Current + 2] << 8);
					Text = Current.ToString("X6") + " CopyAccumulatorToAbsoluteAddressPlusYIndex " + address.ToString("X4");
					Instruction = "Write";
					Address = address.ToString("X4") + "+Y";
					Next = Current + 3;
					break;

				case 0x9a:
					Text = Current.ToString("X6") + " CopyXIndexToStackPointer";
					Next = Current + 1;
					break;

				case 0x9b:
					Text = Current.ToString("X6") + " CopyXIndexToYIndex";
					Next = Current + 1;
					break;

				case 0x9c:
					address = Data[Current + 1] | (Data[Current + 2] << 8);
					Text = Current.ToString("X6") + " SetAbsoluteAddressToZero " + address.ToString("X4");
					Instruction = "Write";
					Address = address.ToString("X4");
					Next = Current + 3;
					break;

				case 0x9d:
					address = Data[Current + 1] | (Data[Current + 2] << 8);
					Text = Current.ToString("X6") + " CopyAccumulatorToAbsoluteAddressPlusXIndex " + address.ToString("X4");
					Instruction = "Write";
					Address = address.ToString("X4") + "+X";
					Next = Current + 3;
					break;

				case 0x9e:
					address = Data[Current + 1] | (Data[Current + 2] << 8);
					Text = Current.ToString("X6") + " SetAbsoluteAddressPlusXIndexToZero " + address.ToString("X4");
					Instruction = "Write";
					Address = address.ToString("X4") + "+X";
					Next = Current + 3;
					break;

				case 0x9f:
					address = Data[Current + 1] | Data[Current + 2] << 8 | Data[Current + 3] << 16;
					Text = Current.ToString("X6") + " CopyAccumulatorToAbsoluteLongAddressPlusXIndex " + address.ToString("X6");
					Instruction = "Write";
					Address = address.ToString("X6") + "+X";
					Next = Current + 4;
					break;

				case 0xa0:
					if ((Flags & 0x10) == 0)
					{
						value = Data[Current + 1] | Data[Current + 2] << 8;
						Text = Current.ToString("X6") + " CopyImmediateToYIndex " + value.ToString("X4");
						Next = Current + 3;
					}
					else
					{
						value = Data[Current + 1];
						Text = Current.ToString("X6") + " CopyImmediateToYIndex " + value.ToString("X2");
						Next = Current + 2;
					}
					break;

				case 0xa2:
					if ((Flags & 0x10) == 0)
					{
						value = Data[Current + 1] | Data[Current + 2] << 8;
						Text = Current.ToString("X6") + " CopyImmediateToXIndex " + value.ToString("X4");
						Next = Current + 3;
					}
					else
					{
						value = Data[Current + 1];
						Text = Current.ToString("X6") + " CopyImmediateToXIndex " + value.ToString("X2");
						Next = Current + 2;
					}
					break;

				case 0xa5:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " CopyDirectAddressToAccumulator " + address.ToString("X2");
					Instruction = "Read";
					Address = address.ToString("X2");
					Next = Current + 2;
					break;

				case 0xa6:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " CopyDirectAddressToXIndex " + address.ToString("X2");
					Instruction = "Read";
					Address = address.ToString("X2");
					Next = Current + 2;
					break;

				case 0xa8:
					Text = Current.ToString("X6") + " CopyAccumulatorToYIndex";
					Next = Current + 1;
					break;

				case 0xa9:
					if ((Flags & 0x20) == 0)
					{
						value = Data[Current + 1] | Data[Current + 2] << 8;
						Text = Current.ToString("X6") + " CopyImmediateToAccumulator " + value.ToString("X4");
						Next = Current + 3;
					}
					else
					{
						value = Data[Current + 1];
						Text = Current.ToString("X6") + " CopyImmediateToAccumulator " + value.ToString("X2");
						Next = Current + 2;
					}
					break;

				case 0xaa:
					Text = Current.ToString("X6") + " CopyAccumulatorToXIndex";
					Next = Current + 1;
					break;

				case 0xab:
					Text = Current.ToString("X6") + " PullDataBank";
					Next = Current + 1;
					break;

				case 0xac:
					address = Data[Current + 1] | Data[Current + 2] << 8;
					Text = Current.ToString("X6") + " CopyAbsoluteAddressToYIndex " + address.ToString("X4");
					Instruction = "Read";
					Address = address.ToString("X4");
					Next = Current + 3;
					break;

				case 0xad:
					address = Data[Current + 1] | (Data[Current + 2] << 8);
					Text = Current.ToString("X6") + " CopyAbsoluteAddressToAccumulator " + address.ToString("X4");
					Instruction = "Read";
					Address = address.ToString("X4");
					Next = Current + 3;
					break;

				case 0xae:
					address = Data[Current + 1] | (Data[Current + 2] << 8);
					Text = Current.ToString("X6") + " CopyAbsoluteAddressToXIndex " + address.ToString("X4");
					Instruction = "Read";
					Address = address.ToString("X4");
					Next = Current + 3;
					break;

				case 0xaf:
					address = Data[Current + 1] | Data[Current + 2] << 8 | Data[Current + 3] << 16;
					Text = Current.ToString("X6") + " CopyAbsoluteLongAddressToAccumulator " + address.ToString("X6");
					Instruction = "Read";
					Address = address.ToString("X6");
					Next = Current + 4;
					break;

				case 0xb0:
					address = Current + 2 + (sbyte)Data[Current + 1];
					Text = Current.ToString("X6") + " BranchToRelativeIfGreaterOrEqual " + address.ToString("X6");
					Instruction = "Branch";
					Next = Current + 2;
					Branch = address;
					break;

				case 0xb5:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " CopyDirectAddressPlusXIndexToAccumulator " + address.ToString("X2");
					Instruction = "Read";
					Address = address.ToString("X2") + "+X";
					Next = Current + 2;
					break;

				case 0xb7:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " CopyDirectAddressLongPointerPlusYIndexToAccumulator " + address.ToString("X2");
					Instruction = "Read";
					Address = "[" + address.ToString("X2") + "]+Y";
					Next = Current + 2;
					break;

				case 0xb9:
					address = Data[Current + 1] | (Data[Current + 2] << 8);
					Text = Current.ToString("X6") + " CopyAbsoluteAddressPlusYIndexToAccumulator " + address.ToString("X4");
					Instruction = "Read";
					Address = address.ToString("X4") + "+Y";
					Next = Current + 3;
					break;

				case 0xbb:
					Text = Current.ToString("X6") + " CopyYIndexToXIndex";
					Next = Current + 1;
					break;

				case 0xbd:
					address = Data[Current + 1] | (Data[Current + 2] << 8);
					Text = Current.ToString("X6") + " CopyAbsoluteAddressPlusXIndexToAccumulator " + address.ToString("X4");
					Instruction = "Read";
					Address = address.ToString("X4") + "+X";
					Next = Current + 3;
					break;

				case 0xbf:
					address = Data[Current + 1] | Data[Current + 2] << 8 | Data[Current + 3] << 16;
					Text = Current.ToString("X6") + " CopyAbsoluteLongAddressPlusXIndexToAccumulator " + address.ToString("X6");
					Instruction = "Read";
					Address = address.ToString("X6") + "+X";
					Next = Current + 4;
					break;

				case 0xc0:
					if ((Flags & 0x10) == 0)
					{
						value = Data[Current + 1] | Data[Current + 2] << 8;
						Text = Current.ToString("X6") + " CompareYIndexToImmediate " + value.ToString("X4");
						Next = Current + 3;
					}
					else
					{
						value = Data[Current + 1];
						Text = Current.ToString("X6") + " CompareYIndexToImmediate " + value.ToString("X2");
						Next = Current + 2;
					}
					break;

				case 0xc2:
					value = Data[Current + 1];
					Text = Current.ToString("X6") + " ClearImmediateFlags " + value.ToString("X2");
					Next = Current + 2;
					Flags &= (byte)~value;
					break;

				case 0xc5:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " CompareAccumulatorToDirectAddress " + address.ToString("X2");
					Instruction = "Read";
					Address = address.ToString("X2");
					Next = Current + 2;
					break;

				case 0xc8:
					Text = Current.ToString("X6") + " IncrementYIndex";
					Next = Current + 1;
					break;

				case 0xc9:
					if ((Flags & 0x20) == 0)
					{
						value = Data[Current + 1] | Data[Current + 2] << 8;
						Text = Current.ToString("X6") + " CompareAccumulatorToImmediate " + value.ToString("X4");
						Next = Current + 3;
					}
					else
					{
						value = Data[Current + 1];
						Text = Current.ToString("X6") + " CompareAccumulatorToImmediate " + value.ToString("X2");
						Next = Current + 2;
					}
					break;

				case 0xca:
					Text = Current.ToString("X6") + " DecrementXIndex";
					Next = Current + 1;
					break;

				case 0xcd:
					address = Data[Current + 1] | (Data[Current + 2] << 8);
					Text = Current.ToString("X6") + " CompareAccumulatorToAbsoluteAddress " + address.ToString("X4");
					Instruction = "Read";
					Address = address.ToString("X4");
					Next = Current + 3;
					break;

				case 0xce:
					address = Data[Current + 1] | (Data[Current + 2] << 8);
					Text = Current.ToString("X6") + " DecrementAbsoluteAddress " + address.ToString("X4");
					Instruction = "Write";
					Address = address.ToString("X4");
					Next = Current + 3;
					break;

				case 0xd0:
					address = Current + 2 + (sbyte)Data[Current + 1];
					Text = Current.ToString("X6") + " BranchToRelativeIfNotEqual " + address.ToString("X6");
					Instruction = "Branch";
					Next = Current + 2;
					Branch = address;
					break;

				case 0xd4:
					value = Data[Current + 1];
					Text = Current.ToString("X6") + " PushPointer " + value.ToString("X2");
					Next = Current + 2;
					break;

				case 0xda:
					Text = Current.ToString("X6") + " PushXIndex";
					Next = Current + 1;
					break;

				case 0xe0:
					if ((Flags & 0x10) == 0)
					{
						value = Data[Current + 1] | Data[Current + 2] << 8;
						Text = Current.ToString("X6") + " CompareXIndexToImmediate " + value.ToString("X4");
						Next = Current + 3;
					}
					else
					{
						value = Data[Current + 1];
						Text = Current.ToString("X6") + " CompareXIndexToImmediate " + value.ToString("X2");
						Next = Current + 2;
					}
					break;

				case 0xe2:
					value = Data[Current + 1];
					Text = Current.ToString("X6") + " SetImmediateFlags " + value.ToString("X2");
					Next = Current + 2;
					Flags |= (byte)value;
					break;

				case 0xe5:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " SubtractDirectAddressFromAccumulator " + address.ToString("X2");
					Instruction = "Read";
					Address = address.ToString("X2");
					Next = Current + 2;
					break;

				case 0xe6:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " IncrementDirectAddress " + address.ToString("X2");
					Instruction = "Write";
					Address = address.ToString("X2");
					Next = Current + 2;
					break;

				case 0xe8:
					Text = Current.ToString("X6") + " IncrementXIndex";
					Next = Current + 1;
					break;

				case 0xe9:
					if ((Flags & 0x20) == 0)
					{
						value = Data[Current + 1] | Data[Current + 2] << 8;
						Text = Current.ToString("X6") + " SubtractImmediateFromAccumulator " + value.ToString("X4");
						Next = Current + 3;
					}
					else
					{
						value = Data[Current + 1];
						Text = Current.ToString("X6") + " SubtractImmediateFromAccumulator " + value.ToString("X2");
						Next = Current + 2;
					}
					break;

				case 0xeb:
					Text = Current.ToString("X6") + " ExchangeAccumulators";
					Next = Current + 1;
					break;

				case 0xec:
					address = Data[Current + 1] | (Data[Current + 2] << 8);
					Text = Current.ToString("X6") + " CompareIndexXToAbsoluteAddress " + address.ToString("X4");
					Instruction = "Read";
					Address = address.ToString("X4");
					Next = Current + 3;
					break;

				case 0xee:
					address = Data[Current + 1] | (Data[Current + 2] << 8);
					Text = Current.ToString("X6") + " IncrementAbsoluteAddress " + address.ToString("X4");
					Instruction = "Write";
					Address = address.ToString("X4");
					Next = Current + 3;
					break;

				case 0xf0:
					address = Current + 2 + (sbyte)Data[Current + 1];
					Text = Current.ToString("X6") + " BranchToRelativeIfEqual " + address.ToString("X6");
					Instruction = "Branch";
					Next = Current + 2;
					Branch = address;
					break;

				case 0xf4:
					address = Data[Current + 1] | Data[Current + 2] << 8;
					Text = Current.ToString("X6") + " PushImmediate " + address.ToString("X4");
					Next = Current + 3;
					break;

				case 0xf5:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " SubtractDirectAddressPlusXIndexFromAccumulator " + address.ToString("X2");
					Instruction = "Read";
					Address = address.ToString("X2") + "+X";
					Next = Current + 2;
					break;

				case 0xfa:
					Text = Current.ToString("X6") + " PullXIndex";
					Next = Current + 1;
					break;

				case 0xfb:
					Text = Current.ToString("X6") + " ExchangeCarryFlagWithEmulationFlag";
					Next = Current + 1;
					break;

				case 0xfd:
					address = Data[Current + 1] | (Data[Current + 2] << 8);
					Text = Current.ToString("X6") + " SubtractAbsoluteAddressPlusXIndexFromAccumulator " + address.ToString("X4");
					Instruction = "Read";
					Address = address.ToString("X4") + "+X";
					Next = Current + 3;
					break;

				case 0xff:
					address = Data[Current + 1] | Data[Current + 2] << 8 | Data[Current + 3] << 16;
					Text = Current.ToString("X6") + " SubtractAbsoluteLongAddressPlusXIndexFromAccumulator " + address.ToString("X6");
					Instruction = "Read";
					Address = address.ToString("X6") + "+X";
					Next = Current + 4;
					break;

				default:
					Text = Current.ToString("X6") + " Unknown Instruction: " + instruction.ToString("X2");
					break;
			}
		}

		private static void ReadFile(string fileName)
		{
			Data = new byte[16 * 1024 * 1024];

			using (var stream = File.OpenRead(fileName))
			{
				for (var bank = 0x00; bank < 0x7E; bank++)
				{
					stream.Position = bank * 0x8000;
					stream.Read(Data, (bank * 0x10000) + 0x8000, 0x8000);
				}

				for (var bank = 0xFE; bank < 0x100; bank++)
				{
					stream.Position = bank * 0x8000;
					stream.Read(Data, (bank * 0x10000) + 0x8000, 0x8000);
				}

				Array.Copy(Data, 0, Data, 0x800000, 0x7E0000);
			}
		}
	}
}
