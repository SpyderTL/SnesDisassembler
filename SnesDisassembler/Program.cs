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
					case "b":
						Current = Branch;
						break;

					case "c":
						Stack.Push(Next);
						Current = Branch;
						break;

					default:
						int.TryParse(fields[0], System.Globalization.NumberStyles.HexNumber, null, out var address);
						Current = address;
						break;
				}
			}
		}

		private static void Disassemble()
		{
			var instruction = Data[Current];

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
					Next = Current + 2;
					break;

				case 0x06:
					address = (int)Data[Current + 1];
					Text = Current.ToString("X6") + " ShiftDirectAddressLeft " + address.ToString("X2");
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
					Next = Current + 2;
					Branch = address;
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
					Next = Current + 3;
					Branch = address;
					break;

				case 0x21:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " AndAccumulatorWithDirectAddressPlusXIndexPointer " + address.ToString("X2");
					Next = Current + 2;
					break;

				case 0x22:
					address = Data[Current + 1] | (Data[Current + 2] << 8) | (Data[Current + 3] << 16);
					Text = Current.ToString("X6") + " CallAbsoluteLongAddress " + address.ToString("X6");
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
					Next = Current + 2;
					Branch = address;
					break;

				case 0x32:
					address = (int)Data[Current + 1];
					Text = Current.ToString("X6") + " AndAccumulatorWithDirectAddressPointer " + address.ToString("X2");
					Next = Current + 2;
					break;

				case 0x38:
					Text = Current.ToString("X6") + " SetCarryFlag";
					Next = Current + 1;
					break;

				case 0x39:
					address = Data[Current + 1] | Data[Current + 2] << 8;
					Text = Current.ToString("X6") + " AndAccumulatorWithAbsoluteAddressPlusYIndex " + address.ToString("X4");
					Next = Current + 3;
					break;

				case 0x3a:
					Text = Current.ToString("X6") + " DecrementAccumulator";
					Next = Current + 1;
					break;

				case 0x3d:
					address = Data[Current + 1] | Data[Current + 2] << 8;
					Text = Current.ToString("X6") + " AndAccumulatorWithAbsoluteAddressPlusXIndex " + address.ToString("X4");
					Next = Current + 3;
					break;

				case 0x40:
					Text = Current.ToString("X6") + " ReturnFromInterrupt";
					break;

				case 0x46:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " ShiftDirectAddressRight " + address.ToString("X2");
					Next = Current + 2;
					break;

				case 0x48:
					Text = Current.ToString("X6") + " PushAccumulator";
					Next = Current + 1;
					break;

				case 0x4a:
					Text = Current.ToString("X6") + " ShiftAccumulatorRight";
					Next = Current + 1;
					break;

				case 0x4b:
					Text = Current.ToString("X6") + " PushProgramBank";
					Next = Current + 1;
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
					Text = Current.ToString("X6") + " CopyAccumulatorToDirectPage";
					Next = Current + 1;
					break;

				case 0x5c:
					address = Data[Current + 1] | Data[Current + 2] << 8 | Data[Current + 3] << 16;
					Text = Current.ToString("X6") + " JumpToAbsoluteLongAddress " + address.ToString("X6");
					Next = address;
					break;

				case 0x60:
					Text = Current.ToString("X6") + " ReturnToCaller";
					Next = Stack.Pop();
					break;

				case 0x6b:
					Text = Current.ToString("X6") + " ReturnToLongCaller";
					Next = Stack.Pop();
					break;

				case 0x63:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " AddStackRelativeAddressToAccumulator " + address.ToString("X2");
					Next = Current + 2;
					break;

				case 0x64:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " SetDirectAddressToZero " + address.ToString("X2");
					Next = Current + 2;
					break;

				case 0x65:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " AddDirectAddressToAccumulator " + address.ToString("X2");
					Next = Current + 2;
					break;

				case 0x67:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " AddDirectAddressLongPointerToAccumulator " + address.ToString("X2");
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
					Next = Current + 4;
					break;

				case 0x80:
					address = Current + 2 + (sbyte)Data[Current + 1];
					Text = Current.ToString("X6") + " JumpToRelative " + address.ToString("X6");
					Next = address;
					break;

				case 0x82:
					address = Current + 3 + BitConverter.ToInt16(Data, Current + 1);
					Text = Current.ToString("X6") + " JumpToRelativeLong " + address.ToString("X6");
					Next = address;
					break;

				case 0x85:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " CopyAccumulatorToDirectAddress " + address.ToString("X2");
					Next = Current + 2;
					break;

				case 0x86:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " CopyXIndexToDirectAddress " + address.ToString("X2");
					Next = Current + 2;
					break;

				case 0x88:
					Text = Current.ToString("X6") + " DecrementYIndex";
					Next = Current + 1;
					break;

				case 0x89:
					value = Data[Current + 1];
					Text = Current.ToString("X6") + " TestImmediate " + value.ToString("X2");
					Next = Current + 2;
					break;

				case 0x8a:
					Text = Current.ToString("X6") + " CopyXIndexToAccumulator";
					Next = Current + 1;
					break;

				case 0x8c:
					address = Data[Current + 1] | Data[Current + 2] << 8;
					Text = Current.ToString("X6") + " CopyYIndexToAbsoluteAddress " + address.ToString("X4");
					Next = Current + 3;
					break;

				case 0x8d:
					address = Data[Current + 1] | Data[Current + 2] << 8;
					Text = Current.ToString("X6") + " CopyAccumulatorToAbsoluteAddress " + address.ToString("X4");
					Next = Current + 3;
					break;

				case 0x8e:
					address = Data[Current + 1] | Data[Current + 2] << 8;
					Text = Current.ToString("X6") + " CopyXIndexToAbsoluteAddress " + address.ToString("X4");
					Next = Current + 3;
					break;

				case 0x8f:
					address = Data[Current + 1] | Data[Current + 2] << 8 | Data[Current + 3] << 16;
					Text = Current.ToString("X6") + " CopyAccumulatorToAbsoluteLongAddress " + address.ToString("X6");
					Next = Current + 4;
					break;

				case 0x90:
					address = Current + 2 + (sbyte)Data[Current + 1];
					Text = Current.ToString("X6") + " BranchToRelativeIfLessThan " + address.ToString("X6");
					Next = Current + 2;
					Branch = address;
					break;

				case 0x98:
					Text = Current.ToString("X6") + " CopyYIndexToAccumulator";
					Next = Current + 1;
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
					address = Data[Current + 1] | Data[Current + 2] << 8;
					Text = Current.ToString("X6") + " SetAbsoluteAddressToZero " + address.ToString("X4");
					Next = Current + 3;
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
					Next = Current + 2;
					break;

				case 0xa6:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " CopyDirectAddressToXIndex " + address.ToString("X2");
					Next = Current + 2;
					break;

				case 0xac:
					address = Data[Current + 1] | Data[Current + 2] << 8;
					Text = Current.ToString("X6") + " CopyAbsoluteAddressToYIndex " + address.ToString("X4");
					Next = Current + 3;
					break;

				case 0xaf:
					address = Data[Current + 1] | Data[Current + 2] << 8 | Data[Current + 3] << 16;
					Text = Current.ToString("X6") + " CopyAbsoluteLongAddressToAccumulator " + address.ToString("X6");
					Next = Current + 4;
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

				case 0xad:
					address = Data[Current + 1] | Data[Current + 2] << 8;
					Text = Current.ToString("X6") + " CopyAbsoluteAddressToAccumulator " + address.ToString("X4");
					Next = Current + 3;
					break;

				case 0xae:
					address = Data[Current + 1] | Data[Current + 2] << 8;
					Text = Current.ToString("X6") + " CopyAbsoluteAddressToXIndex " + address.ToString("X4");
					Next = Current + 3;
					break;

				case 0xb0:
					address = Current + 2 + (sbyte)Data[Current + 1];
					Text = Current.ToString("X6") + " BranchToRelativeIfGreaterOrEqual " + address.ToString("X6");
					Next = Current + 2;
					Branch = address;
					break;

				case 0xb5:
					address = Data[Current + 1];
					Text = Current.ToString("X6") + " CopyDirectAddressPlusXIndexToAccumulator " + address.ToString("X2");
					Next = Current + 2;
					break;

				case 0xbb:
					Text = Current.ToString("X6") + " CopyYIndexToXIndex";
					Next = Current + 1;
					break;

				case 0xbd:
					address = Data[Current + 1] | Data[Current + 2] << 8;
					Text = Current.ToString("X6") + " CopyAbsoluteAddressPlusXIndexToAccumulator " + address.ToString("X4");
					Next = Current + 3;
					break;

				case 0xbf:
					address = Data[Current + 1] | Data[Current + 2] << 8 | Data[Current + 3] << 16;
					Text = Current.ToString("X6") + " CopyAbsoluteLongAddressPlusXIndexToAccumulator " + address.ToString("X6");
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
					Next = Current + 2;
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
					address = Data[Current + 1] | Data[Current + 2] << 8;
					Text = Current.ToString("X6") + " CompareAccumulatorToAbsoluteAddress " + address.ToString("X4");
					Next = Current + 3;
					break;

				case 0xce:
					address = Data[Current + 1] | Data[Current + 2] << 8;
					Text = Current.ToString("X6") + " DecrementAbsoluteAddress " + address.ToString("X4");
					Next = Current + 3;
					break;

				case 0xd0:
					address = Current + 2 + (sbyte)Data[Current + 1];
					Text = Current.ToString("X6") + " BranchToRelativeIfNotEqual " + address.ToString("X6");
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
					address = Data[Current + 1] | Data[Current + 2] << 8;
					Text = Current.ToString("X6") + " CompareIndexXToAbsoluteAddress " + address.ToString("X4");
					Next = Current + 3;
					break;

				case 0xee:
					address = Data[Current + 1] | Data[Current + 2] << 8;
					Text = Current.ToString("X6") + " IncrementAbsoluteAddress " + address.ToString("X4");
					Next = Current + 3;
					break;

				case 0xf0:
					address = Current + 2 + (sbyte)Data[Current + 1];
					Text = Current.ToString("X6") + " BranchToRelativeIfEqual " + address.ToString("X6");
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
					address = Data[Current + 1] | Data[Current + 2] << 8;
					Text = Current.ToString("X6") + " SubtractAbsoluteAddressPlusXIndexFromAccumulator " + address.ToString("X4");
					Next = Current + 3;
					break;

				case 0xff:
					address = Data[Current + 1] | Data[Current + 2] << 8 | Data[Current + 3] << 16;
					Text = Current.ToString("X6") + " SubtractAbsoluteLongAddressPlusXIndexFromAccumulator " + address.ToString("X6");
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
