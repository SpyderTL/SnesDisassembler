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

				case 0x18:
					Text = Current.ToString("X6") + " ClearCarryFlag";
					Next = Current + 1;
					break;

				case 0x20:
					var address = Data[Current + 1] | Data[Current + 2] << 8;
					Text = Current.ToString("X6") + " CallAbsoluteAddress " + address.ToString("X6");
					Next = Current + 3;
					Branch = address;
					break;

				case 0x30:
					address = Current + 2 + Data[Current + 1];
					Text = Current.ToString("X6") + " BranchToRelativeIfNegative " + address.ToString("X6");
					Next = Current + 2;
					Branch = address;
					break;

				case 0x40:
					Text = "ReturnFromInterrupt";
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

				case 0x78:
					Text = Current.ToString("X6") + " SetInterruptDisableFlag";
					Next = Current + 1;
					break;

				case 0x9a:
					Text = Current.ToString("X6") + " CopyXIndexToStackPointer";
					Next = Current + 1;
					break;

				case 0xa2:
					var value = Data[Current + 1] | Data[Current + 2] << 8;
					Text = Current.ToString("X6") + " CopyImmediateToXIndex " + value.ToString("X4");
					Next = Current + 3;
					break;

				case 0xa9:
					value = Data[Current + 1] | Data[Current + 2] << 8;
					Text = Current.ToString("X6") + " CopyImmediateToAccumulator " + value.ToString("X4");
					Next = Current + 3;
					break;

				case 0xab:
					Text = Current.ToString("X6") + " PullDataBank";
					Next = Current + 1;
					break;

				case 0xc2:
					value = Data[Current + 1];
					Text = Current.ToString("X6") + " ClearImmediateFlags " + value.ToString("X2");
					Next = Current + 2;
					break;

				case 0xd4:
					value = Data[Current + 1];
					Text = Current.ToString("X6") + " PushPointer " + value.ToString("X2");
					Next = Current + 2;
					break;

				case 0xe2:
					value = Data[Current + 1];
					Text = Current.ToString("X6") + " SetImmediateFlags " + value.ToString("X2");
					Next = Current + 3;
					break;

				case 0xec:
					address = Data[Current + 1] | Data[Current + 2] << 8;
					Text = Current.ToString("X6") + " CompareIndexXToAbsoluteAddress " + address.ToString("X4");
					Next = Current + 3;
					break;

				case 0xfb:
					Text = Current.ToString("X6") + " ExchangeCarryFlagWithEmulationFlag";
					Next = Current + 1;
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
